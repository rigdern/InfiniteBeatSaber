' The purpose of this wrapper is to run the MSBuild target InfiniteBeatSaber:Eval
' without launching a terminal window. When executing MSBuild directly as an "External Tool"
' in Visual Studio, a terminal window briefly appears that disappears when MSBuild exits. It's
' annoying. Using `msbuildEval.vbs` as an "External Tool" in Visual Studio instead prevents
' the terminal window from appearing.
'
' The MSBuild target InfiniteBeatSaber:Eval:
'   - Compiles `EvalProgram.cs` into a DLL.
'   - Sends the DLL to a websocket running in the mod. The mod evaluates the DLL
'     which gives a REPL-like experience.

Set WshShell = CreateObject("WScript.Shell")

Dim msbuildPath
msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe"
Dim args
args = "-target:InfiniteBeatSaber:Eval"

WshShell.Run """" & msbuildPath & """ " & args, 0, True
Set WshShell = Nothing
