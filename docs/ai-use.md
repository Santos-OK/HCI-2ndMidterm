AI usage log
One entry for each relevant use of AI. Every teammate adds their own entries (copy the table below and add the next number).
Understand what you submit: be ready to explain every script in the video.
Entries by Alvaro Romero. Everything marked `TODO` must be completed with what really happened before submitting.
Index
#	Date	Teammate	Topic
1	2026-10-11	Alvaro Romero	Figma kiosk turned into a Unity Canvas app
2	2026-10-11	Alvaro Romero	Git login for the private repository
3	2026-10-11	Alvaro Romero	Splitting the work by teammate and writing `docs/design.md`
4	2026-10-12	Alvaro Romero	UI moved into the scene so it can be styled in the editor
5	2026-10-12	Omar Santos	Hamburger menu with "Reset availability"
6	2026-10-12	Omar Santos	Persistence of kits and loans between sessions
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Entry 1 – Unity implementation of the kiosk
Field	Detail
Tool	Claude (Claude Code / Claude desktop app)
Date	2026-10-11
Teammate	Alvaro Romero
Purpose	Turn the Project 01 Figma kiosk into a working Unity app with a Canvas (uGUI) UI and both scenarios.
Prompt	"puedes checar proyectos de figma?? es para pasartelo y que despues me lo hagas en unity", with the Figma link (PartialProject01), the course instructions and my answer "usemos canvas". Claude read the Figma file through the Figma connector.
Response used	The C# scripts in `Assets/Scripts/Kiosk/`: data model (`KioskModels.cs`), rules (`KioskService.cs`: availability, kit verification, checkout, reservation), UI helpers (`UiKit.cs`) and one method per screen (`KioskApp.cs`, `KioskApp.Screens.cs`). Screen positions were copied from the 402 x 874 Figma frames.
Changes made by the team	TODO (write what you changed, fixed or rewrote). Things I decided myself: use Canvas, keep the Figma frame size, which parts of the generated code to keep or remove.
Verification	Claude could not run Unity. I opened the project in Unity 6000.5.4f1, played the full flow and compared each screen with the Figma frames: it behaved like the Figma. TODO: add the compile errors you fixed, if any.
Related work	PR #1 (`feature/funcionamiento-Alvaro`). TODO: add commit links.
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Entry 2 – Git login for the private repository
Field	Detail
Tool	Claude
Date	2026-10-11
Teammate	Alvaro Romero
Purpose	Fix the "Password authentication is not supported" error when cloning the private repository, and log in from Git Bash with my own account.
Prompt	Screenshot of the error and "como me logeo desde el git bash para que sepa que estoy desde mi cuenta y asi poder descargarlo?".
Response used	Log in through the browser (Git Credential Manager) instead of typing a password.
Changes made by the team	Used the browser login.
Verification	The clone worked after the browser login ("Authentication Succeeded").
Related work	Not applicable.
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Entry 3 – Splitting the work by teammate and writing `docs/design.md`
Field	Detail
Tool	Claude
Date	2026-10-11
Teammate	Alvaro Romero
Purpose	Keep only my own tasks (app flow, edit-kit function, design.md) and remove work that belongs to other teammates, then draft `docs/design.md`.
Prompt	Screenshot of the team task sheet and "No quiero que hagas tareas de alguien mas, si ya hiciste algo que no me corresponde, hay que quitarlo, para que mis compañeros hagan su trabajo".
Response used	The trimmed project (no visual styling, no test panel, no extra docs) and the first draft of `docs/design.md`: three interaction decisions (status shown with color, word and time; verification blocks incomplete kits; Scenario B explanation and reservation), the Figma vs Unity table and the list of images to add.
Changes made by the team	TODO (what you rewrote in design.md, the real screenshots you added, the decisions you changed).
Verification	I checked that each decision matches what the app really does when played. TODO: confirm the screenshots in `docs/images/` are real captures.
Related work	TODO (PR / commit links).
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Entry 4 – UI moved into the scene so it can be styled in the editor
Field	Detail
Tool	Claude
Date	2026-10-12
Teammate	Alvaro Romero
Purpose	Let the teammate in charge of the visual style edit the interface in the Unity editor. The UI was only created by code at Play, so the scene looked empty.
Prompt	"puedes agregar a la escena algunos elementos como cuadrados y textos para que mi compañero los pueda identificar mejor?? que puedas hacerlos mediante un canva, botones texto y lo que se necesite."
Response used	`KioskLayout.cs` (creates the Canvas with nine screens as real GameObjects), `KioskSceneBuilder.cs` (menu "Quest Kiosk > Build UI in open scene" and "Show screen") and a rewrite of `KioskApp.cs`, `KioskApp.Screens.cs` and `UiKit.cs` so the code finds the scene objects by name and only writes data and handles taps. A message for the teammate explaining the object names that must not be renamed.
Changes made by the team	TODO (what you changed). The visual style itself was done by the teammate in the scene.
Verification	Claude could not compile it. TODO: write that you ran "Build UI in open scene", saved the scene, played the flow and that the console had no errors.
Related work	TODO (PR / commit links, the style branch of the teammate).
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Entry 5 – Hamburger menu with "Reset availability"
Field	Detail
Tool	Claude
Date	2026-10-12
Teammate	Omar Santos
Purpose	Add a way to reset the demo: a hamburger button at the top right of the Main screen that opens a menu with "Reset availability", which makes all kits available again. Tapping the hamburger again closes the menu.
Prompt	"Me puedes ayudar a crear un boton en la esquina superior derecha de la pantalla main que sirva resetear los devices y que todos aparezcan como disponibles? ... un boton con el icono del menu de hamburguesa ... que despliegue un boton que diga "reset availability"" (with the teammate's `ResetControls.cs`).
Response used	`KioskApp.ResetMenu.cs` (builds the hamburger and the menu, toggles it, resets every kit to Available and clears reservations, holds and the current request) and `KioskResetMenuEditor.cs` (menu "Quest Kiosk > Add reset menu (hamburger)" to put the objects in the scene). The teammate's `ResetControls.cs` was not used directly because it depends on a `KitInventory` class that does not exist in this project; only its idea (a reset action) was kept.
Changes made by the team	TODO (what you changed, how the teammate styled the button).
Verification	Claude could not compile it. TODO: write your test (borrow a kit, open the menu, close it, press reset, see "6 of 6 kits available").
Related work	Teammate's `ResetControls.cs`. TODO: PR / commit links.
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Entry 6 – Persistence of kits and loans between sessions
Field	Detail
Tool	Claude
Date	2026-10-12
Teammate	Omar Santos
Purpose	When the app is closed and opened again, keep the number of available kits and the information of the kits and loans.
Prompt	"Ahora necesito darle persistencia. Con esto me refiero a que si el usuario cierra la apliación al volver a abrirla se mantenga la misma cantidad de dispositivos disponibles y toda la informacion pertinente tanto a estos como a los que están en préstamo."
Response used	`KioskPersistence.cs` (saves the inventory as JSON in `kiosk_state.json` inside Unity's persistent data folder: it loads at start, saves when something changes and when the app closes; times are saved as absolute dates so expired loans are released after loading) and `KioskSaveMenu.cs` (menu "Quest Kiosk > Saved data" to delete the file or open its folder for testing).
Changes made by the team	TODO (what you changed).
Verification	Claude could not compile or run it. TODO: write your test (borrow a kit, stop Play, play again and check "5 of 6" with the kit as Loaned; same with a reservation and with reset; test the built executable) and add the evidence to `docs/testing.md` (case "saved data after restart").
Related work	TODO (PR / commit links).
---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
What the AI-generated code does
Script	What it does
`KioskModels.cs`	Plain data: kit status, kit, loan, whole inventory and the result of checking a kit's contents.
`KioskService.cs`	The rules, without UI: which kits can be borrowed, content check (1 headset, 2 controllers, 1 charger), checkout, reservation with a 15 minute hold, and releasing kits when their time passes.
`UiKit.cs`	Helpers to create boxes, texts, buttons and a scroll list, and to find scene objects by name.
`KioskLayout.cs`	Creates the Canvas with the nine screens as normal objects, once, from the editor menu.
`KioskApp.cs`, `KioskApp.Screens.cs`	Controller: shows one screen at a time, writes the real data (status, times, locker) into the scene objects and handles taps.
`KioskApp.ResetMenu.cs`	Hamburger menu and "Reset availability".
`KioskPersistence.cs`	Saves and loads the inventory between sessions.
`KioskSceneBuilder.cs`, `KioskResetMenuEditor.cs`, `KioskSaveMenu.cs`	Editor-only menus ("Quest Kiosk") to build the UI, add the reset menu and manage saved data.
