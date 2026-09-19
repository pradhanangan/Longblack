# Generate Stock Take Items at the Draft → InProgress transition, not at creation

A Stock Take's scope is chosen at creation via optional Brand/Category filters, but the actual `StockTakeItem` rows (and their snapshotted `ExpectedQuantity`) are not materialized until the Stock Take transitions from `Draft` to `InProgress`. While in `Draft`, only the scope filter itself is stored — there is no item list yet.

We chose this over eagerly generating items at creation time (with `ExpectedQuantity` filled in later) because a Stock Take can sit in `Draft` for a while before counting actually starts, and an eagerly-generated item list would risk going stale against the catalogue and current Inventory in the meantime. Deferring both "what's in scope" and "what did the system expect" to the single moment counting begins keeps that snapshot meaningfully accurate to when it's actually used.

The cost: a `Draft` Stock Take cannot show a preview item list or item count before counting starts — only its scope filter (Brand/Category) is visible. If a future workflow needs to preview the resolved item list while still in `Draft`, this decision will need revisiting.
