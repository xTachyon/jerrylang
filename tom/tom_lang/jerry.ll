; ModuleID = 'jerry'
source_filename = "jerry"

declare void @println(i8*)

define void @main() {
  %x = alloca i64, align 8
  store i64 5, i64* %x, align 4
}
