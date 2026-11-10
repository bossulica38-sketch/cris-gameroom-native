Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"

!define APP_NAME "Jocuri și Discuții"
!define APP_EXE "JocuriSiDiscutii.exe"
!define UPDATER_EXE "CrisGameRoomUpdater.exe"
!define VERSION "1.0.9"
!define PUBLISHER "Cris GameRoom"
!define INSTALL_DIR "$LOCALAPPDATA\Programs\Cris GameRoom\Jocuri și Discuții (x86)"

Name "${APP_NAME}"
OutFile "${OUTFILE}"
InstallDir "${INSTALL_DIR}"
InstallDirRegKey HKCU "Software\Cris GameRoom\Jocuri și Discuții" "InstallDir"
RequestExecutionLevel user
VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "CompanyName" "${PUBLISHER}"
VIAddVersionKey "FileDescription" "${APP_NAME}"
VIAddVersionKey "LegalCopyright" "Copyright © 2026 CRIS GameRoom. Toate drepturile rezervate."
VIAddVersionKey "FileVersion" "${VERSION}"

!define MUI_ABORTWARNING
!define MUI_LANGDLL_REGISTRY_ROOT HKCU
!define MUI_LANGDLL_REGISTRY_KEY "Software\Cris GameRoom\Jocuri și Discuții"
!define MUI_LANGDLL_REGISTRY_VALUENAME "InstallerLanguage"
!define MUI_LANGDLL_DEFAULT "Romanian"


!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "$(RUN_APP)"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "Romanian"
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Portuguese"
!insertmacro MUI_LANGUAGE "Turkish"
!insertmacro MUI_LANGUAGE "Italian"
!insertmacro MUI_LANGUAGE "Spanish"
!insertmacro MUI_LANGUAGE "German"
!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "Dutch"
!insertmacro MUI_LANGUAGE "Polish"

Function .onInit
  ExecWait '"$SYSDIR\taskkill.exe" /F /T /IM "${APP_EXE}"'
  ExecWait '"$SYSDIR\taskkill.exe" /F /T /IM "${UPDATER_EXE}"'
  !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd






LangString SEC_MAIN ${LANG_ROMANIAN} "Jocuri și Discuții"
LangString SEC_DESKTOP ${LANG_ROMANIAN} "Pictogramă pe desktop"
LangString RUN_APP ${LANG_ROMANIAN} "Pornește Jocuri și Discuții"
LangString SEC_MAIN ${LANG_ENGLISH} "Games and Discussions"
LangString SEC_DESKTOP ${LANG_ENGLISH} "Desktop shortcut"
LangString RUN_APP ${LANG_ENGLISH} "Start Games and Discussions"
LangString SEC_MAIN ${LANG_PORTUGUESE} "Jogos e Conversas"
LangString SEC_DESKTOP ${LANG_PORTUGUESE} "Atalho no ambiente de trabalho"
LangString RUN_APP ${LANG_PORTUGUESE} "Iniciar Jogos e Conversas"
LangString SEC_MAIN ${LANG_TURKISH} "Oyunlar ve Sohbetler"
LangString SEC_DESKTOP ${LANG_TURKISH} "Masaüstü kısayolu"
LangString RUN_APP ${LANG_TURKISH} "Oyunlar ve Sohbetleri başlat"
LangString SEC_MAIN ${LANG_ITALIAN} "Giochi e conversazioni"
LangString SEC_DESKTOP ${LANG_ITALIAN} "Collegamento sul desktop"
LangString RUN_APP ${LANG_ITALIAN} "Avvia Giochi e conversazioni"
LangString SEC_MAIN ${LANG_SPANISH} "Juegos y conversaciones"
LangString SEC_DESKTOP ${LANG_SPANISH} "Acceso directo en el escritorio"
LangString RUN_APP ${LANG_SPANISH} "Iniciar Juegos y conversaciones"
LangString SEC_MAIN ${LANG_GERMAN} "Spiele und Gespräche"
LangString SEC_DESKTOP ${LANG_GERMAN} "Desktop-Verknüpfung"
LangString RUN_APP ${LANG_GERMAN} "Spiele und Gespräche starten"
LangString SEC_MAIN ${LANG_FRENCH} "Jeux et discussions"
LangString SEC_DESKTOP ${LANG_FRENCH} "Raccourci sur le bureau"
LangString RUN_APP ${LANG_FRENCH} "Lancer Jeux et discussions"
LangString SEC_MAIN ${LANG_DUTCH} "Spellen en gesprekken"
LangString SEC_DESKTOP ${LANG_DUTCH} "Snelkoppeling op bureaublad"
LangString RUN_APP ${LANG_DUTCH} "Spellen en gesprekken starten"
LangString SEC_MAIN ${LANG_POLISH} "Gry i rozmowy"
LangString SEC_DESKTOP ${LANG_POLISH} "Skrót na pulpicie"
LangString RUN_APP ${LANG_POLISH} "Uruchom Gry i rozmowy"

Section "$(SEC_MAIN)" SEC_MAIN
  SectionIn RO
  SetRegView 32
  SetOutPath "$INSTDIR"

  File "${STAGING}\JocuriSiDiscutii.exe"

  SetOutPath "$INSTDIR\updater"
  File "${STAGING}\updater\CrisGameRoomUpdater.exe"

  SetOutPath "$INSTDIR"

  WriteRegStr HKCU "Software\Cris GameRoom\Jocuri și Discuții" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Cris GameRoom\Jocuri și Discuții" "Version" "${VERSION}"

  ; Limba selectată la instalare devine limba inițială a aplicației.
  StrCpy $0 "ro"
  ${If} $LANGUAGE == 1033
    StrCpy $0 "en"
  ${ElseIf} $LANGUAGE == 2070
    StrCpy $0 "pt"
  ${ElseIf} $LANGUAGE == 1055
    StrCpy $0 "tr"
  ${ElseIf} $LANGUAGE == 1040
    StrCpy $0 "it"
  ${ElseIf} $LANGUAGE == 1034
    StrCpy $0 "es"
  ${ElseIf} $LANGUAGE == 1031
    StrCpy $0 "de"
  ${ElseIf} $LANGUAGE == 1036
    StrCpy $0 "fr"
  ${ElseIf} $LANGUAGE == 1043
    StrCpy $0 "nl"
  ${ElseIf} $LANGUAGE == 1045
    StrCpy $0 "pl"
  ${EndIf}
  CreateDirectory "$LOCALAPPDATA\CrisGameRoom"
  FileOpen $1 "$LOCALAPPDATA\CrisGameRoom\language.txt" w
  FileWrite $1 "$0"
  FileClose $1

  CreateDirectory "$SMPROGRAMS\Cris GameRoom"
  CreateShortcut "$SMPROGRAMS\Cris GameRoom\Jocuri și Discuții.lnk" "$INSTDIR\${APP_EXE}"

  CreateDirectory "$APPDATA\Microsoft\Windows\Start Menu\Programs\Cris GameRoom"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "$(SEC_DESKTOP)" SEC_DESKTOP
  CreateShortcut "$DESKTOP\Jocuri și Discuții.lnk" "$INSTDIR\${APP_EXE}"
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\Jocuri și Discuții.lnk"
  Delete "$SMPROGRAMS\Cris GameRoom\Jocuri și Discuții.lnk"
  Delete "$APPDATA\Microsoft\Windows\Start Menu\Programs\Cris GameRoom\Jocuri și Discuții.lnk"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "$INSTDIR\${APP_EXE}"
  Delete "$INSTDIR\updater\${UPDATER_EXE}"
  RMDir "$INSTDIR\updater"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Cris GameRoom\Jocuri și Discuții"
SectionEnd
