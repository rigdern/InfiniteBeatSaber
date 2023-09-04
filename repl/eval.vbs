' The purpose of this wrapper is to run `eval.js` without launching a terminal window.
' When executing `eval.js` directly as an "External Tool" in Visual Studio,
' a terminal window briefly appears that disappears when `eval.js` exits. It's
' annoying. Using `eval.vbs` as an "External Tool" in Visual Studio instead prevents
' the terminal window from appearing.

Set WshShell = CreateObject("WScript.Shell") 
WshShell.Run "node eval.js", 0, True
Set WshShell = Nothing
