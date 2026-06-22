#!/bin/bash
# Provisiona o barramento, as filas das areas e as regras de fan-out no LocalStack.
# Roda automaticamente quando o LocalStack fica "ready".
set -euo pipefail

BUS=krt-onboarding-bus
PATTERN='{"source":["krt.onboarding"]}'

awslocal events create-event-bus --name "$BUS"

# Uma fila por area consumidora; cada uma recebe TODOS os eventos de conta (fan-out).
for AREA in fraude cartoes; do
  QUEUE_URL=$(awslocal sqs create-queue --queue-name "${AREA}-queue" --query QueueUrl --output text)
  QUEUE_ARN=$(awslocal sqs get-queue-attributes \
    --queue-url "$QUEUE_URL" --attribute-names QueueArn \
    --query 'Attributes.QueueArn' --output text)

  awslocal events put-rule \
    --name "${AREA}-rule" --event-bus-name "$BUS" --event-pattern "$PATTERN"

  awslocal events put-targets \
    --rule "${AREA}-rule" --event-bus-name "$BUS" \
    --targets "Id=${AREA},Arn=${QUEUE_ARN}"

  echo "Area '${AREA}': fila e regra criadas."
done

echo "LocalStack pronto: bus=${BUS}; filas=fraude-queue, cartoes-queue"
