if not exist C:\tmp (
mkdir C:\tmp
)

if exist z:\ (
    net use z: /delete /y
)

net use Z: \\t7minecraftstate.file.core.windows.net\state /u:AZURE\t7minecraftstate rzcZ2yF0VjKv21iBlepch5lBBKB2y7Dn8GupduvJuI2ZsjyaKZOjdya3T304EN8bqf8nkEi1QIoh/bTALJhvmw== > error.txt 2>&1

if exist c:\timo (
	rm c:\timo
)

mklink /d "c:\timo" "\\t7minecraftstate.file.core.windows.net\state" > error2.txt 2>&1