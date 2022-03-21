; ModuleID = 'jerry'
source_filename = "jerry"
target datalayout = "e-m:w-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-windows-msvc"

@0 = private unnamed_addr constant [4 x i8] c"abc\00", align 1

declare void @println(i8*)

define void @main() {
  %x = alloca i64, align 8
  store i64 5, i64* %x, align 8
  call void @println(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @0, i32 0, i32 0))
  ret void
}
