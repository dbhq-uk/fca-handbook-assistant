# Live eval report - deployed Azure app

- App: https://ca-fca-dev-uks-01.jollyglacier-4b42e05d.uksouth.azurecontainerapps.io
- Embeddings: Azure text-embedding-3-small (1536)
- Cases: 9
- Refusal correctness: 100%
- Citation recall: 100%

| Question | Expected | Outcome | Cited |
| --- | --- | --- | --- |
| What must a firm do when assessing the suitability of an investment recommendation? | COBS 9.2.1 | answered | COBS 9.2.1 |
| What does the Consumer Duty require firms to do for retail customers? | PRIN 2A.1.1 | answered | PRIN 2A.1.1 |
| Must a financial promotion be fair, clear and not misleading? | COBS 4.2.1 | answered | COBS 4.2.1 |
| What compliance arrangements must a firm maintain? | SYSC 6.1.1 | answered | SYSC 6.1.1 |
| What regular reporting must firms submit to the FCA? | SUP 16.1.1 | answered | SUP 16.1.1 |
| What must a firm do to act with integrity? | PRIN 2.1.1 | answered | PRIN 2.1.1 |
| What is the capital of France? | refuse | refused |  |
| Will it rain tomorrow in London? | refuse | refused |  |
| What is a good recipe for sourdough bread? | refuse | refused |  |
