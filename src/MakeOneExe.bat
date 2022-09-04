REM Requires ILRepack https://github.com/gluck/il-repack (https://www.nuget.org/packages/ILRepack/)
REM Makes fnr.exe, with all assemblies from the output folder merged into it.

set RELEASE_DIR=.\FindAndReplace.App\bin\Release\net461\

cd %RELEASE_DIR%
%NUGET_PACKAGES%\ilrepack\2.0.18\tools\ILRepack.exe /log:log.txt /targetplatform:v4 /log:log.txt /targetplatform:v4 /out:fnr.exe FindAndReplace.App.exe FindAndReplace.dll EncodingTools.dll CommandLine.dll
