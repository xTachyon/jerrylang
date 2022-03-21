; ModuleID = 'jerry'
source_filename = "jerry"

@0 = private unnamed_addr constant [4 x i8] c"abc\00", align 1

declare void @println(i8*)

define void @main() {
  %x = alloca i64, align 8
  store i64 5, i64* %x, align 4
  call void @println(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @0, i32 0, i32 0))
}
