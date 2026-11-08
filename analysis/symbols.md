# Shadows of Doubt v42.04: Verified Symbol Reconnaissance
*Conducted by Ava Sunwoo (The Syojhin Librarian) via MetaDump on BepInEx Interop Assembly-CSharp.dll*  
*Date: November 8, 2026*

---

## 1. Local Player & Avatar
* **Singleton:** `Player.Instance` (property: `Player.get_Instance()`)
* **Camera / Eyes:** `Player.Instance.cam` (Camera), `Player.Instance.camHeightParent`
* **World Transform:** `Player.Instance.transform.position`, `Player.Instance.transform.rotation`
* **Movement & Teleport:** `Player.Instance.SetPosition(Vector3 position, Quaternion rotation)`
* **Stance / Crouch:** `Player.Instance.crouchedTransition` (float), `Player.Instance.OnCrouchedChange()`
* **Speed:** `Player.Instance.desiredWalkSpeed`, `Player.Instance.desiredRunSpeed`

---

## 2. City & Save File Load Pipeline
* **City Data Singleton:** `CityData.Instance`
  * `CityData.Instance.seed` (string seed identifier)
  * `CityData.Instance.cityName` (string name of current generated city)
* **Save State Controller:** `SaveStateController.Instance`
  * `SaveStateController.Instance.CaptureSaveStateAsync(string saveName, bool isAutoSave)` (triggers async disk write)
  * `SaveStateController.Instance.LoadSaveState(StateSaveData data)`
* **City Constructor Engine:** `CityConstructor.Instance`
  * `CityConstructor.Instance.LoadSaveGame()`
  * `CityConstructor.Instance.LoadSaveStateFile()`
  * `CityConstructor.Instance.GenerateCityFromShareCode()`

---

## 3. Save File Paths & Formats (Verified on Disk)
* **Root Folder:** `%USERPROFILE%\AppData\LocalLow\ColePowered Games\Shadows of Doubt\`
* **Save State Folder (`Save/`):**
  * Extension: `.sodb` (State Save Data)
  * Typical Size: ~600 KB
  * Contains player state, case progress, inventory, citizen statuses.
* **City Geometry Folder (`Cities/`):**
  * Extension: `.citb` (City Binary Data)
  * Typical Size: ~7.2 MB
  * Accompanying file: `.txt` metadata (~186 bytes)

---

## 4. Clock, Time & Pause Governance
* **Session Data Singleton:** `SessionData.Instance`
* **Current Game Time:** `SessionData.Instance.gameTime` (float), `SessionData.Instance.decimalClock` (float)
* **Set Game Time:** `SessionData.Instance.SetGameTime(...)`
* **Time Scale:** `SessionData.Instance.SetTimeSpeed(TimeSpeed speed)`
* **Pause Hook:** `SessionData.Instance.PauseGame(bool pause, bool playAudio, bool openPauseMenu)`
* **Toggle Pause:** `SessionData.Instance.TogglePause(bool showMenu)`
* **Co-op Strategy:** Prefix hook on `SessionData.PauseGame` returning `false` while connected in co-op mode to prevent single-player menus from halting game simulation.

---

## 5. Doors & Physical Access
* **Class:** `NewDoor`
* **Open / Close Hook:** `NewDoor.SetOpen(float angle, Actor actor, bool playAudio, float speed)`
* **Lock / Unlock Hook:** `NewDoor.SetLocked(bool locked, Actor actor, bool playAudio)`
* **Door State Fields:** `NewDoor.isLocked` (bool), `NewDoor.isClosed` (bool)
* **Door Identification:** `NewDoor.wall.wallID` (int) or `NewDoor.transform.position`

---

## 6. World Interactables
* **Class:** `Interactable`
* **Action Hook:** `Interactable.OnInteraction(InteractionAction action, Actor actor, bool alt, float val)`
* **Identifier:** `Interactable.id` (int - unique world assignment ID)
* **Physical Pickups:** `Interactable.SetPhysicsPickupState(bool state, Actor actor, ...)`
* **Light / Switches:** `Interactable.SetSwtichByType(Switch type, bool state, Actor actor, ...)`

---

## 7. Case Board, Evidence & Strings
* **Controller Singleton:** `CasePanelController.Instance`
* **Pin Evidence:** `CasePanelController.PinToCasePanel(Case c, Evidence evidence, DataKey key, bool updatePinned, Vector2 position, bool isPinned)`
* **Unpin Evidence:** `CasePanelController.UnPinFromCasePanel(...)`
* **String Connections:** `CasePanelController.FinishCustomStringLinkSelection(PinnedItemController target)`
* **Case Creation:** `CasePanelController.CreateNewCase(...)`

---

## 8. Crime & Murder Simulation
* **Controller Singleton:** `MurderController.Instance`
* **Murder Routine:** `MurderController.TriggerNextMurder()`, `MurderController.ExecuteNewMurder(...)`
* **Co-op Authority Strategy:** 
  * Host simulates `MurderController.Update()`.
  * Client hooks prefix `MurderController.Update()` returning `false` to suppress client-side murder generation, receiving `CrimeEvent` packets from the host instead.
