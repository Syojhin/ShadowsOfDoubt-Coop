import re

path_ui = r"D:\Modding_Lab\projects\Shadows_of_Doubt\external_sodcoop\repo\SODCoop\Ui.cs"
with open(path_ui, "r", encoding="utf-8") as f:
    text = f.read()

target = """        internal static string T(string en, string r) { return ru ? r : en; }
        static TMPro.TextMeshProUGUI settingsText;
        static bool IsRu(string s) { foreach (char ch in s ?? "") if (ch >= 'Ѐ' && ch <= 'ӿ') return true; return false; }

        static void LanguageCheck()
        {
            if (settingsText == null) return;
            bool r = IsRu(settingsText.text);
            if (r == ru) return;
            ru = r;
            SetText(mpBtn, T("Multiplayer", "Мультиплеер")); SetText(chBtn, T("Character", "Персонаж"));
            Plugin.L.LogInfo("[UI] язык меню: " + (ru ? "русский" : "английский"));
        }"""

replacement = """        static bool de;
        static readonly System.Collections.Generic.Dictionary<string, string> GermanMap = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Multiplayer"] = "Mehrspieler",
            ["Character"] = "Charakter",
            ["Host a game"] = "Spiel hosten",
            ["Direct connect"] = "Direktverbindung",
            ["Password"] = "Passwort",
            ["Server rules"] = "Serverregeln",
            ["Game modifiers"] = "Spiel-Modifikatoren",
            ["Server name"] = "Servername",
            ["Port"] = "Port",
            ["Host IP address"] = "Host-IP-Adresse",
            ["Password (if the server has one)"] = "Passwort (falls vorhanden)",
            ["Connect"] = "Verbinden",
            ["CONNECT"] = "VERBINDEN",
            ["CONNECTING"] = "VERBINDE...",
            ["CONNECTED"] = "VERBUNDEN",
            ["CONNECTED to "] = "VERBUNDEN mit ",
            ["HOST"] = "HOST",
            ["HOSTING"] = "HOST LÄUFT",
            ["BACK"] = "ZURÜCK",
            ["Back"] = "Zurück",
            ["REFRESH"] = "AKTUALISIEREN",
            ["DIRECT"] = "DIREKTVERBINDUNG",
            ["DISABLE MULTIPLAYER"] = "MEHRSPIELER DEAKTIVIEREN",
            ["Save now"] = "Jetzt speichern",
            ["Save profile"] = "Profil speichern",
            ["Quit game"] = "Spiel beenden",
            ["Mod updated"] = "Mod aktualisiert",
            ["Update failed"] = "Update fehlgeschlagen",
            ["Later"] = "Später",
            ["Open page"] = "Seite öffnen",
            ["Update"] = "Aktualisieren",
            ["Hide"] = "Ausblenden",
            ["Can't connect"] = "Verbindung fehlgeschlagen",
            ["REJECTED by "] = "ABGELEHNT von ",
            ["CONNECTION LOST, reconnecting to "] = "VERBINDUNG VERLOREN, verbinde neu mit ",
            [" (wrong password?)"] = " (falsches Passwort?)",
            [" players"] = " Spieler",
            [", waiting"] = ", warte...",
            ["(End = stop)"] = "(Ende = Stopp)",
            ["(End = leave multiplayer)"] = "(Ende = Verlassen)",
            ["No servers found yet. The host must have loaded a city. Use Direct connect to type an address."] = "Noch keine Server gefunden. Der Host muss eine Stadt geladen haben. Nutze Direktverbindung für die IP.",
            ["Enter the host IP address."] = "Gib die Host-IP-Adresse ein.",
            ["Port must be a number from 1024 to 65535."] = "Port muss eine Zahl zwischen 1024 und 65535 sein.",
            ["Free hospital"] = "Kostenloses Krankenhaus",
            ["Wallets"] = "Geldbeutel",
            ["Fast elevators"] = "Schnelle Aufzüge",
            ["Autosave"] = "Automatisches Speichern",
            ["Time"] = "Zeit",
            ["Gender"] = "Geschlecht",
            ["Male"] = "Männlich",
            ["Female"] = "Weiblich",
            ["Non-binary"] = "Nicht-binär",
            ["Skin tone"] = "Hautton",
            ["Hair style"] = "Frisur",
            ["Hair color"] = "Haarfarbe",
            ["Clothing"] = "Kleidung",
            ["Trenchcoat"] = "Trenchcoat",
            ["Hat"] = "Hut",
            ["Glasses"] = "Brille",
            ["First name"] = "Vorname",
            ["Surname"] = "Nachname",
            ["Randomize"] = "Zufällig",
            ["Done"] = "Fertig",
            ["Cancel"] = "Abbrechen",
            ["Apply"] = "Übernehmen"
        };

        internal static string T(string en, string r)
        {
            if (de && GermanMap.TryGetValue(en, out string? deStr)) return deStr;
            return ru ? r : en;
        }

        static TMPro.TextMeshProUGUI settingsText;
        static bool IsRu(string s) { foreach (char ch in s ?? "") if (ch >= 'Ѐ' && ch <= 'ӿ') return true; return false; }
        static bool IsDe(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s.IndexOf("Einstellung", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            try
            {
                if (MainMenuController.Instance != null && MainMenuController.Instance.loadedLanguage != null)
                {
                    string lang = MainMenuController.Instance.loadedLanguage.ToLowerInvariant();
                    if (lang.Contains("german") || lang.Contains("deutsch")) return true;
                }
            }
            catch {}
            return false;
        }

        static void LanguageCheck()
        {
            if (settingsText == null) return;
            string txt = settingsText.text ?? "";
            bool r = IsRu(txt);
            bool d = IsDe(txt);
            if (r == ru && d == de) return;
            ru = r;
            de = d;
            SetText(mpBtn, T("Multiplayer", "Мультиплеер"));
            SetText(chBtn, T("Character", "Персонаж"));
            Plugin.L.LogInfo("[UI] Menü-Sprache: " + (de ? "Deutsch" : ru ? "Russisch" : "Englisch"));
        }"""

assert target in text, "Target block in Ui.cs not found"
text = text.replace(target, replacement, 1)

with open(path_ui, "w", encoding="utf-8") as f:
    f.write(text)

print("Ui.cs German localization applied successfully.")
