#!/bin/bash
# 超神机械师模组预编译检查脚本
# 用法：bash precompile_check.sh

MOD_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPILE_DIR="/tmp/sm_compile_check"
REF_DIR="/home/user/Doubao/chats/38443538092133890/gz_ref/02_хОЯчЙИхПНч╝ЦшпС"
DOTNET="$HOME/dotnet/dotnet"

echo "=== 超神机械师模组预编译检查 ==="
echo ""

# 1. 语法检查（dotnet build）
echo "[1/3] 语法检查..."
rm -rf "$COMPILE_DIR"
mkdir -p "$COMPILE_DIR"
cd "$COMPILE_DIR"
$DOTNET new classlib -n SMCheck --framework net6.0 -q 2>/dev/null
rm -f SMCheck/Class1.cs
cp -r "$MOD_DIR/Code" SMCheck/
cp "$MOD_DIR/Main.cs" SMCheck/
cd SMCheck
SYNTAX_ERRORS=$($DOTNET build 2>&1 | grep "error CS" | grep -v "CS0246" | grep -v "CS0103" | grep -v "CS0234" | wc -l)
if [ "$SYNTAX_ERRORS" -eq 0 ]; then
    echo "  ✅ 语法检查通过（0个语法错误）"
else
    echo "  ❌ 发现 $SYNTAX_ERRORS 个语法错误："
    $DOTNET build 2>&1 | grep "error CS" | grep -v "CS0246" | grep -v "CS0103" | grep -v "CS0234" | head -20
fi

# 2. 括号平衡检查
echo ""
echo "[2/3] 括号平衡检查..."
BRACKET_ERRORS=0
for f in $(find "$MOD_DIR/Code" -name "*.cs"); do
    OPEN=$(grep -o "{" "$f" | wc -l)
    CLOSE=$(grep -o "}" "$f" | wc -l)
    if [ "$OPEN" -ne "$CLOSE" ]; then
        echo "  ❌ $(basename $f): {=$OPEN }=$CLOSE"
        BRACKET_ERRORS=$((BRACKET_ERRORS+1))
    fi
done
if [ "$BRACKET_ERRORS" -eq 0 ]; then
    echo "  ✅ 所有文件括号平衡"
fi

# 3. API存在性检查（对照反编译文件）
echo ""
echo "[3/3] API存在性检查（对照反编译文件）..."
APIS=("ButtonsViewer" "setIconValue" "showStatsRows" "getLocaleID" "addPower" "LocalizedTextManager" "AssetManager" "BaseStatAsset" "ActorTraitGroupAsset" "GodPower")
API_ERRORS=0
for api in "${APIS[@]}"; do
    COUNT=$(grep -rl "$api" "$REF_DIR" 2>/dev/null | wc -l)
    if [ "$COUNT" -eq 0 ]; then
        echo "  ⚠️  $api: 未在反编译文件中找到（可能是自定义API）"
    else
        echo "  ✅ $api: $COUNT 个文件包含"
    fi
done

echo ""
echo "=== 检查完成 ==="
echo "语法错误: $SYNTAX_ERRORS | 括号错误: $BRACKET_ERRORS"
if [ "$SYNTAX_ERRORS" -eq 0 ] && [ "$BRACKET_ERRORS" -eq 0 ]; then
    echo "✅ 可以打包发布"
else
    echo "❌ 请修复上述问题后再打包"
fi
