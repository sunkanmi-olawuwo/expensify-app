# Tech Debt

1. Soft delete (still a real gap)
Hard delete via Remove() is in use everywhere. There's no DeletedAtUtc or IsDeleted flag on expenses or income. A user who accidentally deletes an expense has no recovery path. Given this is financial data, soft delete is worth revisiting - even a simple 30-day recycle bin.

2. Rate limiting (still missing)
Confirmed absent from the codebase. Even before AI chat lands, the write endpoints (expense creation, etc.) have no throttling. This should be added before production launch - ASP.NET Core's built-in RateLimiter middleware would be a low-effort addition.

3. Subscriptions, AI chat, and data export
All three are confirmed unimplemented - these are known future epics per the PRD, so not bugs, just pending work.

4. Email/scheduled notifications
SignalR real-time notifications are implemented well via the transactional outbox + SignalR pattern. But there are no email or scheduled reminders. When subscriptions land, users will need out-of-band nudges (e.g., "import your subscriptions for this month").

5. Deletion of categories with expenses
The Restrict FK behavior on category deletion is correct, but the error surfaced to the user when they try to delete a used category may be opaque (a raw FK violation unless explicitly handled). Worth verifying the API returns a clean, user-facing error message rather than a 500.
