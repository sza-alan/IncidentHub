# Payments API Runbook

## Common symptoms

- Increased HTTP 5xx responses
- Payment processing timeout
- Failed transactions
- Increased response latency

## Investigation

1. Check the payment gateway availability.
2. Check recent 5xx error rate.
3. Check communication timeout with the payment provider.
4. Verify pending payment processing.
5. Check whether failures started after a recent deployment.

## Important

Do not assume that a payment failure means the payment was not processed.
Before retrying payment operations, verify whether the original transaction
was completed to avoid duplicate charges.