# 04: Email a Purchase Order and mark as sent

**What to build:** A Manager or Admin can email the PDF to the Supplier (recipient defaults to the Supplier email, editable, optional CC), resend it, or manually mark it as sent. Every send attempt is logged. See `.scratch/purchase-orders/spec.md`.

**Blocked by:** 03.

**Status:** done

- [ ] `IEmailSender` abstraction with an SMTP implementation; settings from configuration and secrets, nothing real committed
- [ ] Send endpoint writes a send log record (user, time, recipient, CC, outcome, error) and updates `SentAt`/`SentBy` on success
- [ ] Mark-sent endpoint sets `SentAt`/`SentBy` without emailing
- [ ] Resend allowed; failures surfaced clearly and retryable
- [ ] Receiving is never blocked by whether a Purchase Order was sent
- [ ] Detail screen: Send dialog (recipient, CC), Mark sent, send history
- [ ] Builds and lint pass; verified manually
