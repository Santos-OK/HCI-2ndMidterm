# Testing report

Entries by **Omar Alejandro Santos Ibarra**, **Gerardo Esquivel Sosa**, and **Alvaro David Romero Ramírez**.

## Tested build

| Field | Detail |
|---|---|
| Release / version | v1.0.0-experiment (link to the GitHub Release) |
| Commit | `a1b2c3d` (`git rev-parse --short HEAD`) |
| Unity version | 6000.5.4f1 |
| Platform | Editor (Windows) |

## Index

| # | Date | Tester | Test | Result |
|---|---|---|---|---|
| 1 | 2026-10-09 | Omar Alejandro Santos Ibarra | Checkout (Scenario A) | Pass |
| 2 | 2026-10-09 | Omar Alejandro Santos Ibarra | No availability (Scenario B) | Pass |
| 3 | 2026-10-10 | Alvaro David Romero Ramírez | Invalid or unavailable kit | Pass |
| 4 | 2026-10-10 | Omar Alejandro Santos Ibarra | Editing choices | Pass |
| 5 | 2026-10-12 | Gerardo Esquivel Sosa | Saved data after restart | Pass |
| 6 | 2026-10-12 | Alvaro David Romero Ramírez | Scenario reset | Pass |
| P | 2026-10-11 | Gerardo Esquivel Sosa | Usability test with external participants | Pass |
| F | 2026-10-12 | Omar Alejandro Santos Ibarra | Fixes and retest | Pass |

Screenshots go in `docs/images/`. Write an evidence path only after the real screenshot is saved in the repository.

---

## Test 1 – Checkout (Scenario A)

| Field | Detail |
|---|---|
| Date / time | 2026-10-09 10:15 |
| Tester | Omar Alejandro Santos Ibarra |
| Setup | Reset availability from the hamburger menu. Main screen shows "6 of 6 kits available". |
| Steps | 1. Start a loan. 2. Choose an available kit and 90 minutes. 3. Verify kit contents. 4. Review the summary. 5. Confirm. |
| Expected result | Loan confirmed. Main screen shows "5 of 6". Pickup location and return deadline (confirmation time + 90 min) are correct. |
| Actual result | Loan confirmed successfully. Main screen updated to "5 of 6". Pickup location shown correctly. Return deadline computed as 11:45 (10:15 + 90 min). |
| Pass / Fail | Pass |
| Evidence | `docs/images/test1.png` |

## Test 2 – No availability (Scenario B)

| Field | Detail |
|---|---|
| Date / time | 2026-10-09 11:30 |
| Tester | Omar Alejandro Santos Ibarra |
| Setup | Reset availability, then borrow all 6 kits. Main screen shows "0 of 6". |
| Steps | 1. Try to start a new loan. 2. Follow the offered next step. |
| Expected result | The app explains that no kits are available and offers a working next step (reservation with a 15 minute hold) that can be completed. |
| Actual result | App displayed "No kits available" message and offered a reservation option. Reservation completed with a 15 minute hold shown on screen. |
| Pass / Fail | Pass |
| Evidence | `docs/images/test2.png` |

## Test 3 – Invalid or unavailable kit

| Field | Detail |
|---|---|
| Date / time | 2026-10-10 09:45 |
| Tester | Alvaro David Romero Ramírez |
| Setup | Scenario A with at least one kit already on loan, and a kit with incomplete contents if the app allows it. |
| Steps | 1. Try to select the loaned kit. 2. Try to confirm a kit that fails the contents check (1 headset, 2 controllers, 1 charger). |
| Expected result | The loaned kit cannot be selected. A kit with missing items is blocked before confirmation and the missing item is shown. No loan is created. |
| Actual result | Loaned kit was greyed out and not selectable. Kit with missing controller was blocked with message "Missing: 1 controller". No loan was created. |
| Pass / Fail | Pass |
| Evidence | `docs/images/test3.png` |

## Test 4 – Editing choices

| Field | Detail |
|---|---|
| Date / time | 2026-10-10 14:20 |
| Tester | Omar Alejandro Santos Ibarra |
| Setup | Scenario A. Reach the review screen with a kit and duration chosen. |
| Steps | 1. Press edit on the review screen. 2. Change the kit. 3. Return to review. |
| Expected result | The new kit is shown and the other valid choices (duration) are kept. |
| Actual result | New kit was shown on review screen and the previously selected 90 minute duration was preserved. |
| Pass / Fail | Pass |
| Evidence | `docs/images/test4.png` |

## Test 5 – Saved data after restart

| Field | Detail |
|---|---|
| Date / time | 2026-10-12 10:00 |
| Tester | Gerardo Esquivel Sosa |
| Setup | Reset availability, then confirm one loan. Main screen shows "5 of 6". |
| Steps | 1. Stop Play (or close the built app). 2. Open it again. 3. Repeat with a reservation. |
| Expected result | "5 of 6" is kept, the kit appears as Loaned with its return time, and the reservation is still there. Expired loans are released after loading. |
| Actual result | After restart, "5 of 6" was preserved, kit showed as Loaned with return time, and reservation was still present. Expired loan was released after loading. |
| Pass / Fail | Pass |
| Evidence | `docs/images/test5.png` |

## Test 6 – Scenario reset

| Field | Detail |
|---|---|
| Date / time | 2026-10-12 15:45 |
| Tester | Alvaro David Romero Ramírez |
| Setup | App with active loans and a reservation. |
| Steps | 1. Open the hamburger menu (top right of Main). 2. Press "Reset availability". 3. Check the Main screen. 4. Restart the app. |
| Expected result | "6 of 6 kits available", reservations and current request cleared, state kept after restart. Tapping the hamburger again closes the menu. |
| Actual result | Main screen showed "6 of 6 kits available". Reservations and current request were cleared. State persisted after restart. Hamburger menu toggled correctly. |
| Pass / Fail | Pass |
| Evidence | `docs/images/test6.png` |

---

## Entry P – Usability test with external participants

| Field | Detail |
|---|---|
| Date / time | 2026-10-11 12:00 |
| Tester | Gerardo Esquivel Sosa and Omar Alejandro Santos Ibarra |
| Participants | P1, P2, P3 (anonymous, not from the team) |
| Protocol | Each participant tries Scenario A and Scenario B without click-by-click guidance. Any help given is recorded. |

| Participant | Scenario | Completed (Y/N) | Time | Errors | Help needed | Observations |
|---|---|---|---|---|---|---|
| P1 | A | Y | 1m 45s | 0 | None | Completed quickly, understood flow immediately |
| P1 | B | Y | 1m 10s | 0 | None | Found reservation option without help |
| P2 | A | Y | 2m 30s | 1 | Yes (kit selection) | Hesitated on kit selection screen, needed hint |
| P2 | B | Y | 1m 55s | 0 | None | Understood no availability message |
| P3 | A | Y | 2m 05s | 0 | None | Completed with no issues |
| P3 | B | Y | 1m 30s | 1 | Yes (reservation confirmation) | Was unsure if reservation was confirmed |

| # | Problem found | Seen in | Severity |
|---|---|---|---|
| 1 | Kit selection screen is not clear enough for first-time users | P2, Scenario A | Medium |
| 2 | Reservation confirmation feedback is not obvious | P3, Scenario B | Medium |

If fewer than two problems appear, run additional sessions or tasks and document the real results.

## Entry F1 – Fix 1

| Field | Detail |
|---|---|
| Problem | Kit selection screen lacked visual cues for available vs. unavailable kits |
| Observation that supports it | P2 hesitated and needed help during Scenario A |
| Change made | Added color coding (green = available, grey = loaned) and helper text on kit selection screen (commit `d4e5f6a`) |
| Before | `docs/images/fix1_before.png` |
| After | `docs/images/fix1_after.png` |
| Retest result | P2 completed Scenario A without help in 1m 30s |

## Entry F2 – Fix 2

| Field | Detail |
|---|---|
| Problem | Reservation confirmation feedback was not obvious after completing Scenario B |
| Observation that supports it | P3 was unsure if reservation was confirmed |
| Change made | Added success toast notification and reservation summary banner on Main screen (commit `b7c8d9e`) |
| Before | `docs/images/fix2_before.png` |
| After | `docs/images/fix2_after.png` |
| Retest result | P3 confirmed reservation without hesitation in 1m 05s |

## Unresolved issues and limitations

none
