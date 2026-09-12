# Reesaido's Menu Functions — Setup Notes

## What's in this folder
- `MenuItemEntity.cs` — the `MenuItems` table entity + request DTOs
- `MenuFunctions.cs` — the 5 HTTP functions: CreateMenuItem, GetAllMenuItems,
  GetMenuItemsByCategory, UpdateMenuItem, DeleteMenuItem
- `Program.cs`, `host.json`, `CoffeeNChillFunctions.csproj` — standard isolated-worker
  Azure Functions (.NET 8) project scaffolding
- `local.settings.json.example` — copy this to `local.settings.json` (don't commit the real one)

## Before you run it (Visual Studio, .NET 9)
1. Make sure you have **Visual Studio 2022 version 17.12 or later** — older versions don't
   know how to run/debug a .NET 9 Functions app. Check Help → About Microsoft Visual Studio.
2. Make sure the **Azure development** workload is installed (Tools → Get Tools and Features).
3. Open `CoffeeNChillFunctions.csproj` directly, or add the folder to your existing solution
   (right-click solution → Add → Existing Project).
4. Copy `local.settings.json.example` → `local.settings.json` (same folder). Visual Studio
   won't create this for you automatically if you're adding an existing project.
5. Make sure Azurite is running (Thato's container, or the Azurite VS extension/npm package)
   on the default ports (10000/10001/10002) — `UseDevelopmentStorage=true` points at it.
6. Hit **F5** (or the green "Start" button) — Visual Studio will restore NuGet packages and
   launch the Functions host automatically. Watch the console window that pops up for the
   list of function URLs it's listening on.
7. The code calls `CreateIfNotExistsAsync()` on the `MenuItems` table automatically the first
   time any function runs — so nobody has to create the table by hand.

If F5 complains about missing Functions tooling, go to Tools → Options → Azure Functions and
make sure the "Azure Functions Core Tools" version shown is up to date — VS will offer to
download the matching version for .NET 9 the first time you run.

## Message to send Thato
> "Hey Thato — my functions will auto-create the `MenuItems` table the first time they run
> against Azurite (via `CreateIfNotExistsAsync`), so you don't need to pre-create it manually.
> All I need from your side is Azurite up and running on the default ports before I test.
> Once you've got the Azurite container going, can you confirm the table shows up in Azure
> Storage Explorer (or via `az storage table list --connection-string ...`) after I run a
> CreateMenuItem request? That way we've both verified it end-to-end before we merge our
> Postman collections."

## Before you submit
- Test all 5 endpoints yourself against Azurite before merging your Postman requests with Thato's.
