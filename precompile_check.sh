#!/bin/bash
# 超神机械师模组预编译检查脚本 v3
# 用游戏实际DLL(Assembly-CSharp-Publicized) + NML DLL做完整引用检查
# 只过滤CS1069(类型转发缺IMGUIModule，游戏运行时有完整UnityEngine)
# 用法：bash precompile_check.sh

MOD_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPILE_DIR="/tmp/sm_compile_check_v3"
DOTNET="$HOME/dotnet8/dotnet"
DLL_DIR="/home/user/Doubao/chats/38443538092133890/NML_source/bin/Release/net48"

echo "=== 超神机械师模组预编译检查 v3 (完整DLL引用) ==="
echo ""

# 1. 准备编译环境
echo "[1/4] 准备编译环境..."
rm -rf "$COMPILE_DIR"
mkdir -p "$COMPILE_DIR/SMCheck/refs"
cp -r "$MOD_DIR/Code" "$COMPILE_DIR/SMCheck/"
cp "$MOD_DIR/Main.cs" "$COMPILE_DIR/SMCheck/"
cp "$DLL_DIR"/*.dll "$COMPILE_DIR/SMCheck/refs/" 2>/dev/null
echo "  引用DLL: $(ls $COMPILE_DIR/SMCheck/refs/*.dll 2>/dev/null | wc -l) 个"

# 2. 生成.csproj (net48和游戏一致)
echo "[2/4] 生成编译项目..."
cat > "$COMPILE_DIR/SMCheck/SMCheck.csproj" << 'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <NoWarn>CS1069</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="refs\*.dll">
      <HintPath>%(Identity)</HintPath>
    </Reference>
  </ItemGroup>
</Project>
CSPROJ

# 3. 编译
echo "[3/4] 编译检查..."
cd "$COMPILE_DIR/SMCheck"
BUILD_OUTPUT=$($DOTNET build 2>&1)

# 4. 分析错误（只过滤CS1069环境问题）
echo "[4/4] 分析错误..."
REAL_ERRORS=$(echo "$BUILD_OUTPUT" | grep "error CS" | grep -v "CS1069")
ERROR_COUNT=$(echo "$REAL_ERRORS" | grep -c "error CS" || true)
ERROR_COUNT=$(echo "$ERROR_COUNT" | tail -1)

CS1069_COUNT=$(echo "$BUILD_OUTPUT" | grep -c "CS1069" || true)

if [ "$ERROR_COUNT" -eq 0 ] || [ "$ERROR_COUNT" = "0" ]; then
    echo "  ✅ 编译检查通过（0个代码错误）"
    echo "  （已过滤$CS1069_COUNT个CS1069类型转发警告，游戏运行时有完整UnityEngine模块）"
else
    echo "  ❌ 发现 $ERROR_COUNT 个代码错误："
    echo "$REAL_ERRORS" | head -30
fi

# 括号平衡检查
echo ""
echo "=== 括号平衡检查 ==="
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

# API使用模式静态检查
echo ""
echo "=== API使用模式检查 ==="
API_WARNINGS=0

# 检查1: 直接操作PowerButtonSelector.instance.buttons（错误模式）
BAD_BUTTONS=$(grep -rn "PowerButtonSelector.instance.buttons" "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "//")
if [ -n "$BAD_BUTTONS" ]; then
    echo "  ⚠️  发现直接操作PowerButtonSelector.instance.buttons（应用PowerButtonCreator+TabManager）:"
    echo "$BAD_BUTTONS" | head -5
    API_WARNINGS=$((API_WARNINGS+1))
fi

# 检查2: 直接Instantiate PowerButton（应用PowerButtonCreator）
BAD_INSTANTIATE=$(grep -rn "Instantiate.*PowerButton\|Instantiate.*template.*PowerButton" "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "//" | grep -v "StatsIcon")
if [ -n "$BAD_INSTANTIATE" ]; then
    echo "  ⚠️  发现直接Instantiate PowerButton（应用PowerButtonCreator.CreateGodPowerButton）:"
    echo "$BAD_INSTANTIATE" | head -5
    API_WARNINGS=$((API_WARNINGS+1))
fi

# 检查3: 硬编码中文（非日志/注释/模组名称）
BAD_CHINESE=$(grep -rn '[\x{4e00}-\x{9fff}]' "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "Debug.Log\|//\|/\*\|ModName\|\"未知\"" | head -5)
if [ -n "$BAD_CHINESE" ]; then
    echo "  ⚠️  发现可能的硬编码中文UI文本（应用LocalizedTextManager）:"
    echo "$BAD_CHINESE"
    API_WARNINGS=$((API_WARNINGS+1))
fi

# 检查4: 真正的空catch（无日志、无返回值）
BAD_CATCH=$(grep -rn "catch\s*{[^}]*}" "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "Debug\.\|return\|LogService\|//" | grep "catch\s*{}\|catch\s*{\s*}")
if [ -n "$BAD_CATCH" ]; then
    echo "  ⚠️  发现真正的空catch块（应至少记录日志）:"
    echo "$BAD_CATCH" | head -5
    API_WARNINGS=$((API_WARNINGS+1))
fi

if [ "$API_WARNINGS" -eq 0 ]; then
    echo "  ✅ API使用模式检查通过"
fi

echo ""
echo "=== 检查完成 ==="
echo "代码错误: $ERROR_COUNT | 括号错误: $BRACKET_ERRORS | API警告: $API_WARNINGS | CS1069环境警告: $CS1069_COUNT"
if [ "$ERROR_COUNT" -eq 0 ] && [ "$BRACKET_ERRORS" -eq 0 ]; then
    echo "✅ 可以打包发布"
else
    echo "❌ 请修复上述问题后再打包"
fi
