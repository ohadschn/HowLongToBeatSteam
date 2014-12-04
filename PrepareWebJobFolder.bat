rd /s /q %WEBJOBS_DATA_PATH%
md %WEBJOBS_DATA_PATH%

robocopy %R% . /e

IF %ERRORLEVEL% GEQ 8 exit 1
exit 0