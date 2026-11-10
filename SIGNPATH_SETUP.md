# Configurarea semnării gratuite CRIS GameRoom

Proiectul este pregătit pentru integrarea SignPath Foundation. Semnarea rămâne dezactivată până când SignPath aprobă proiectul și sunt configurate identificatoarele reale. Nu se folosesc certificate locale sau chei private în repository.

## Eligibilitate și aprobare

- Repository public: https://github.com/bossulica38-sketch/cris-gameroom-native
- Licență: MIT (`LICENSE`)
- Politica proiectului: `CODE-SIGNING-POLICY.md`
- Cerere: https://signpath.org/apply

Acceptarea este decisă de SignPath Foundation. Licența MIT nu garantează automat aprobarea.

## Configurare în SignPath

1. Creează/activează organizația și proiectul pentru repository-ul de mai sus.
2. Adaugă sistemul de build de încredere GitHub.com și conectează aplicația GitHub SignPath la repository.
3. Creează o politică de semnare pentru lansări, cu aprobare manuală.
4. Configurează două configurații de artefact:
   - `binaries`: tipul rădăcină `zip-file`; semnează recursiv `JocuriSiDiscutii.exe` și `updater/CrisGameRoomUpdater.exe` din arhiva artefactului GitHub Actions.
   - `installer`: tipul rădăcină `zip-file`; semnează executabilul NSIS din arhiva artefactului GitHub Actions.
5. Generează un API token pentru un utilizator care are dreptul de submitter pentru politica de semnare.
6. În configurațiile de artefact, impune metadatele `ProductName = Jocuri și Discuții` și `ProductVersion = 1.0.13` pentru executabilele acestei versiuni. La fiecare lansare, versiunile aplicației, actualizatorului și instalatorului trebuie să fie sincronizate.

## GitHub Actions

În Settings → Secrets and variables → Actions configurează:

Secret:
- `SIGNPATH_API_TOKEN`: tokenul API SignPath. Nu îl publica și nu îl trimite în conversații.

Variables:
- `SIGNPATH_ENABLED` = `true`
- `SIGNPATH_ORGANIZATION_ID` = ID-ul organizației SignPath
- `SIGNPATH_PROJECT_SLUG` = slug-ul proiectului
- `SIGNPATH_SIGNING_POLICY_SLUG` = slug-ul politicii de semnare
- `SIGNPATH_BINARIES_CONFIGURATION_SLUG` = slug-ul configurației `binaries`
- `SIGNPATH_INSTALLER_CONFIGURATION_SLUG` = slug-ul configurației `installer`

Până la configurarea valorilor reale, semnarea rămâne oprită. Workflow-ul construiește instalatoarele x64 și x86; când semnarea este activată, trimite executabilele aplicației/actualizatorului și apoi instalatorul către SignPath. Cererile pot aștepta aprobarea manuală.

## Publicarea versiunilor

Lansările semnate se pornesc manual din GitHub Actions sau printr-un tag `v*`. Verifică ambele arhitecturi și semnăturile Authenticode înainte de copierea fișierelor pe serverul public.
