Unicode true
!include "MUI2.nsh"

!define APP_NAME "Jocuri și Discuții"
!define APP_EXE "JocuriSiDiscutii.exe"
!define UPDATER_EXE "CrisGameRoomUpdater.exe"
!define VERSION "1.0.1"
!define PUBLISHER "Cris GameRoom"
!define INSTALL_DIR "$PROGRAMFILES32\Cris GameRoom\Jocuri și Discuții"
!define ROOT "/home/admin/domains/cris-gameroom-native/CrisGameRoom"
!define STAGING "${ROOT}/installer/staging/win-x86"

Name "${APP_NAME}"
OutFile "${ROOT}/installer/output/JocuriSiDiscutii-Setup-x86.exe"
InstallDir "${INSTALL_DIR}"
InstallDirRegKey HKLM "Software\Cris GameRoom\Jocuri și Discuții" "InstallDir"
RequestExecutionLevel admin
VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "CompanyName" "${PUBLISHER}"
VIAddVersionKey "FileDescription" "${APP_NAME}"
VIAddVersionKey "LegalCopyright" "Copyright © 2026 CRIS GameRoom. Toate drepturile rezervate."
VIAddVersionKey "FileVersion" "${VERSION}"

!define MUI_ABORTWARNING

Function .onInit
  ExecWait '"$SYSDIR\taskkill.exe" /F /T /IM "${APP_EXE}"'
  ExecWait '"$SYSDIR\taskkill.exe" /F /T /IM "${UPDATER_EXE}"'
FunctionEnd

!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Pornește Jocuri și Discuții"

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

Section "Jocuri și Discuții" SEC_MAIN
  SectionIn RO
  SetRegView 32
  SetOutPath "$INSTDIR"

  File "${STAGING}/JocuriSiDiscutii.exe"

  SetOutPath "$INSTDIR\updater"
  File "${STAGING}/updater/CrisGameRoomUpdater.exe"

  SetOutPath "$INSTDIR"

  WriteRegStr HKLM "Software\Cris GameRoom\Jocuri și Discuții" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "Software\Cris GameRoom\Jocuri și Discuții" "Version" "${VERSION}"

  CreateDirectory "$SMPROGRAMS\Cris GameRoom"
  CreateShortcut "$SMPROGRAMS\Cris GameRoom\Jocuri și Discuții.lnk" "$INSTDIR\${APP_EXE}"

  CreateDirectory "$APPDATA\Microsoft\Windows\Start Menu\Programs\Cris GameRoom"
  CreateShortcut "$APPDATA\Microsoft\Windows\Start Menu\Programs\Cris GameRoom\Jocuri și Discuții.lnk" "$INSTDIR\${APP_EXE}"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section /o "Pictogramă pe desktop" SEC_DESKTOP
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
  DeleteRegKey HKLM "Software\Cris GameRoom\Jocuri și Discuții"
SectionEnd
