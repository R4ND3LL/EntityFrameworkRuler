set rootDir=%CD%

echo removing obj folders...
FOR /D /r . %%i in (obj) do @if exist "%%i" rd /s/q "%%i"
echo removing AnyCPU folders...
FOR /D /r . %%i in (AnyCPU) do @if exist "%%i" rd /s/q "%%i"
echo removing Release folders..
FOR /D /r . %%i in (Release) do @if exist "%%i" rd /s/q "%%i"
echo removing Server Bin folder...
rd /s/q "%CD%\Bin\"

cd "%rootDir%"

pause