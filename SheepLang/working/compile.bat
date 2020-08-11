llc code.ll -filetype=obj -o code.obj
cl /EHsc /DEBUG /Z7 /LD code.obj std.cpp