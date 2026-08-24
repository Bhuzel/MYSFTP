; =========================================================================
;  MYSFTP Desktop Suite — Setup Script (NSIS)
;  Build with: makensis MYSFTP-Setup.nsi
;  Produces a proper Windows installer that lets the user pick ANY install
;  drive (C:, D:, etc.), creates Start Menu / Desktop shortcuts, registers
;  an uninstaller in "Add or remove programs", and shows a normal
;  license/progress/finish installer flow — like a "real" desktop app.
; =========================================================================

!define APP_NAME        "MYSFTP"
!define APP_PUBLISHER   "ZellRayy"
!define APP_VERSION     "1.9.1"
!define APP_EXE         "MYSFTP.exe"
!define UNINST_KEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

!include "MUI2.nsh"

Name "${APP_NAME}"
OutFile "MYSFTP-Setup.exe"

; Default suggestion only — the Directory page below lets the user browse to
; and type ANY path on ANY drive, e.g. D:\Apps\MYSFTP.
InstallDir "$PROGRAMFILES64\MYSFTP"
InstallDirRegKey HKLM "${UNINST_KEY}" "InstallLocation"

RequestExecutionLevel admin
SetCompressor /SOLID lzma
Icon "files\app.ico"
UninstallIcon "files\app.ico"

; ---- UI ----
!define MUI_ABORTWARNING
!define MUI_ICON "files\app.ico"
!define MUI_UNICON "files\app.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP_NOSTRETCH
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Jalankan MYSFTP sekarang"
!define MUI_FINISHPAGE_LINK "Kunjungi repository MYSFTP di GitHub"
!define MUI_FINISHPAGE_LINK_LOCATION "https://github.com/Bhuzel/MYSFTP"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "files\LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

; =========================================================================
Section "MYSFTP Desktop Suite (required)" SEC_CORE
  SectionIn RO
  SetOutPath "$INSTDIR"

  File "files\${APP_EXE}"
  File "files\app.ico"
  File "files\Icon.jpg"

  ; Data folder lives right next to the exe wherever the user installed it
  ; (C:\, D:\, a portable drive, etc.) so it's never stuck in a
  ; permission-locked system folder.
  CreateDirectory "$INSTDIR\data"

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Start Menu shortcut
  CreateDirectory "$SMPROGRAMS\MYSFTP"
  CreateShortCut "$SMPROGRAMS\MYSFTP\MYSFTP.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\app.ico"
  CreateShortCut "$SMPROGRAMS\MYSFTP\Uninstall MYSFTP.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\app.ico"

  ; Register in "Add or remove programs" like any real installed app
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "${APP_NAME} — Desktop Suite"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\app.ico"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair" 1
SectionEnd

Section "Desktop Shortcut" SEC_DESKTOP
  CreateShortCut "$DESKTOP\MYSFTP.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\app.ico"
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_CORE} "Berkas inti aplikasi MYSFTP (wajib)."
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC_DESKTOP} "Buat shortcut MYSFTP di Desktop."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; =========================================================================
Section "Uninstall"
  Delete "$INSTDIR\${APP_EXE}"
  Delete "$INSTDIR\app.ico"
  Delete "$INSTDIR\Icon.jpg"
  Delete "$INSTDIR\connections.json"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir /r "$INSTDIR\data"
  RMDir "$INSTDIR"

  Delete "$SMPROGRAMS\MYSFTP\MYSFTP.lnk"
  Delete "$SMPROGRAMS\MYSFTP\Uninstall MYSFTP.lnk"
  RMDir "$SMPROGRAMS\MYSFTP"
  Delete "$DESKTOP\MYSFTP.lnk"

  DeleteRegKey HKLM "${UNINST_KEY}"
SectionEnd
