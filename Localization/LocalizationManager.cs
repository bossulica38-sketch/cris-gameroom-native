using System.Collections.Generic;

namespace CrisGameRoom.Localization;

public static class LocalizationManager
{
    public static string CurrentLanguage { get; private set; } = "ro";

    private static readonly Dictionary<string, Dictionary<string, string>> Languages = new()
    {
        ["ro"] = new()
        {
            ["title"] = "JOCURI ÎN CAMERA",
            ["name"] = "Nume",
            ["password"] = "Parolă",
            ["showPassword"] = "Afișează parola",
            ["rememberPassword"] = "Reține parola",
            ["startWithWindows"] = "Pornește aplicația odată cu Windows",
            ["language"] = "Limbă",
            ["connect"] = "CONECTEAZĂ-TE",
            ["forgotPassword"] = "Ai uitat parola?",
            ["createAccount"] = "Creează cont",
            ["about"] = "Despre noi",
            ["status"] = "Status și servicii",
            ["exit"] = "Ieșire",
            ["connecting"] = "Conectare la serviciu...",
            ["available"] = "Serviciu disponibil",
            ["unavailable"] = "Serviciu indisponibil",
            ["checkingVersion"] = "Verificare versiune...",
            ["version"] = "Versiune",
            ["loginError"] = "Autentificarea a eșuat.",
            ["updateError"] = "Nu s-a putut verifica versiunea aplicației."
        },
        ["en"] = new()
        {
            ["title"] = "GAMES IN THE ROOM",
            ["name"] = "Name",
            ["password"] = "Password",
            ["showPassword"] = "Show password",
            ["rememberPassword"] = "Remember password",
            ["startWithWindows"] = "Start application with Windows",
            ["language"] = "Language",
            ["connect"] = "SIGN IN",
            ["forgotPassword"] = "Forgot password?",
            ["createAccount"] = "Create account",
            ["about"] = "About us",
            ["status"] = "Status and services",
            ["exit"] = "Exit",
            ["connecting"] = "Connecting to service...",
            ["available"] = "Service available",
            ["unavailable"] = "Service unavailable",
            ["checkingVersion"] = "Checking version...",
            ["version"] = "Version",
            ["loginError"] = "Authentication failed.",
            ["updateError"] = "The application version could not be checked."
        },
        ["pt"] = new()
        {
            ["title"] = "JOGOS NA SALA",
            ["name"] = "Nome",
            ["password"] = "Palavra-passe",
            ["showPassword"] = "Mostrar palavra-passe",
            ["rememberPassword"] = "Guardar palavra-passe",
            ["startWithWindows"] = "Iniciar aplicação com o Windows",
            ["language"] = "Idioma",
            ["connect"] = "INICIAR SESSÃO",
            ["forgotPassword"] = "Esqueceu a palavra-passe?",
            ["createAccount"] = "Criar conta",
            ["about"] = "Sobre nós",
            ["status"] = "Estado e serviços",
            ["exit"] = "Sair",
            ["connecting"] = "A ligar ao serviço...",
            ["available"] = "Serviço disponível",
            ["unavailable"] = "Serviço indisponível",
            ["checkingVersion"] = "A verificar a versão...",
            ["version"] = "Versão",
            ["loginError"] = "A autenticação falhou.",
            ["updateError"] = "Não foi possível verificar a versão da aplicação."
        },
        ["tr"] = new()
        {
            ["title"] = "ODA OYUNLARI",
            ["name"] = "Ad",
            ["password"] = "Şifre",
            ["showPassword"] = "Şifreyi göster",
            ["rememberPassword"] = "Şifreyi hatırla",
            ["startWithWindows"] = "Uygulamayı Windows ile başlat",
            ["language"] = "Dil",
            ["connect"] = "GİRİŞ YAP",
            ["forgotPassword"] = "Şifrenizi mi unuttunuz?",
            ["createAccount"] = "Hesap oluştur",
            ["about"] = "Hakkımızda",
            ["status"] = "Durum ve hizmetler",
            ["exit"] = "Çıkış",
            ["connecting"] = "Hizmete bağlanılıyor...",
            ["available"] = "Hizmet kullanılabilir",
            ["unavailable"] = "Hizmet kullanılamıyor",
            ["checkingVersion"] = "Sürüm kontrol ediliyor...",
            ["version"] = "Sürüm",
            ["loginError"] = "Kimlik doğrulama başarısız.",
            ["updateError"] = "Uygulama sürümü kontrol edilemedi."
        },
        ["it"] = new()
        {
            ["title"] = "GIOCHI NELLA STANZA",
            ["name"] = "Nome",
            ["password"] = "Password",
            ["showPassword"] = "Mostra password",
            ["rememberPassword"] = "Ricorda password",
            ["startWithWindows"] = "Avvia l'applicazione con Windows",
            ["language"] = "Lingua",
            ["connect"] = "ACCEDI",
            ["forgotPassword"] = "Password dimenticata?",
            ["createAccount"] = "Crea account",
            ["about"] = "Chi siamo",
            ["status"] = "Stato e servizi",
            ["exit"] = "Esci",
            ["connecting"] = "Connessione al servizio...",
            ["available"] = "Servizio disponibile",
            ["unavailable"] = "Servizio non disponibile",
            ["checkingVersion"] = "Controllo della versione...",
            ["version"] = "Versione",
            ["loginError"] = "Autenticazione non riuscita.",
            ["updateError"] = "Impossibile verificare la versione dell'applicazione."
        },
        ["es"] = new()
        {
            ["title"] = "JUEGOS EN LA SALA",
            ["name"] = "Nombre",
            ["password"] = "Contraseña",
            ["showPassword"] = "Mostrar contraseña",
            ["rememberPassword"] = "Recordar contraseña",
            ["startWithWindows"] = "Iniciar la aplicación con Windows",
            ["language"] = "Idioma",
            ["connect"] = "INICIAR SESIÓN",
            ["forgotPassword"] = "¿Has olvidado la contraseña?",
            ["createAccount"] = "Crear cuenta",
            ["about"] = "Sobre nosotros",
            ["status"] = "Estado y servicios",
            ["exit"] = "Salir",
            ["connecting"] = "Conectando al servicio...",
            ["available"] = "Servicio disponible",
            ["unavailable"] = "Servicio no disponible",
            ["checkingVersion"] = "Comprobando versión...",
            ["version"] = "Versión",
            ["loginError"] = "La autenticación ha fallado.",
            ["updateError"] = "No se pudo comprobar la versión de la aplicación."
        },
        ["de"] = new()
        {
            ["title"] = "SPIELE IM RAUM",
            ["name"] = "Name",
            ["password"] = "Passwort",
            ["showPassword"] = "Passwort anzeigen",
            ["rememberPassword"] = "Passwort speichern",
            ["startWithWindows"] = "Anwendung mit Windows starten",
            ["language"] = "Sprache",
            ["connect"] = "ANMELDEN",
            ["forgotPassword"] = "Passwort vergessen?",
            ["createAccount"] = "Konto erstellen",
            ["about"] = "Über uns",
            ["status"] = "Status und Dienste",
            ["exit"] = "Beenden",
            ["connecting"] = "Verbindung zum Dienst...",
            ["available"] = "Dienst verfügbar",
            ["unavailable"] = "Dienst nicht verfügbar",
            ["checkingVersion"] = "Version wird geprüft...",
            ["version"] = "Version",
            ["loginError"] = "Authentifizierung fehlgeschlagen.",
            ["updateError"] = "Die Anwendungsversion konnte nicht geprüft werden."
        },
        ["fr"] = new()
        {
            ["title"] = "JEUX DANS LA SALLE",
            ["name"] = "Nom",
            ["password"] = "Mot de passe",
            ["showPassword"] = "Afficher le mot de passe",
            ["rememberPassword"] = "Mémoriser le mot de passe",
            ["startWithWindows"] = "Démarrer l'application avec Windows",
            ["language"] = "Langue",
            ["connect"] = "SE CONNECTER",
            ["forgotPassword"] = "Mot de passe oublié ?",
            ["createAccount"] = "Créer un compte",
            ["about"] = "À propos de nous",
            ["status"] = "État et services",
            ["exit"] = "Quitter",
            ["connecting"] = "Connexion au service...",
            ["available"] = "Service disponible",
            ["unavailable"] = "Service indisponible",
            ["checkingVersion"] = "Vérification de la version...",
            ["version"] = "Version",
            ["loginError"] = "Échec de l'authentification.",
            ["updateError"] = "Impossible de vérifier la version de l'application."
        }
    };

    public static void SetLanguage(string code)
    {
        if (Languages.ContainsKey(code))
            CurrentLanguage = code;
    }

    public static string Get(string key)
    {
        if (Languages.TryGetValue(CurrentLanguage, out var language) &&
            language.TryGetValue(key, out var value))
            return value;

        return Languages["ro"].TryGetValue(key, out var fallback)
            ? fallback
            : key;
    }
}
