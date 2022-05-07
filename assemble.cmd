copy /Y run.cmd .\RicherPresence\bin\Release\net6.0\
copy /Y RicherPresence.xml .\RicherPresence\bin\Release\net6.0\
copy /Y ..\DXGIOutputDuplication\x64\Release\*.exe .\RicherPresence\bin\Release\net6.0\
xcopy C:\"Program Files"\Tesseract-OCR\* .\RicherPresence\bin\Release\net6.0\ /Y /S
pause