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

# 检查5: 危险调用模式 - 调用外部方法前没有try-catch保护
# 这些方法内部可能访问null字段，调用时必须有保护
DANGEROUS_METHODS="findNeighbours|findNeighbour|GetComponent|GetComponentInChildren|Instantiate|Resources.Load|AssetManager\..*\.get"
DANGEROUS_CALLS=$(grep -rn "\.\($DANGEROUS_METHODS\)\s*(" "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "try\|catch\|//\|null\s*!=?\|!= null\|== null\|?\." | head -10)
if [ -n "$DANGEROUS_CALLS" ]; then
    echo "  ⚠️  发现危险调用（外部方法可能返回null，建议加try-catch或null检查）:"
    echo "$DANGEROUS_CALLS"
    API_WARNINGS=$((API_WARNINGS+1))
fi

# 检查6: 可能的null引用链 - a.b.c形式没有null检查（排除枚举/静态类/命名空间/数据访问/我们自己的类/枚举赋值/transform）
NULL_CHAIN=$(grep -rnP '\w+\.\w+\.\w+\s*[;=]' "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "//\|try\|catch\|null\|?\.\|System\.\|UnityEngine\.\|NeoModLoader\.\|SuperMech\.\|Code\.\|get_\|set_\|typeof\|nameof\|Mathf\.\|Debug\.\|LogService\.\|Resources\.\|AssetManager\.\|World\.\|Time\.\|GUI\.\|GUILayout\.\|PlayerConfig\.\|Config\.\|TalentType\.\|ProfessionType\.\|ClassMech\|ClassMartial\|ClassPsi\|ClassMage\|ClassMind\|All\.\|Count\b\|\.Data\.\|\.data\.\|\.sanctuary\.\|frame\.\|RootRt\|ContentParent\|\.Type\.\|\.FitMode\.\|\.ScaleMode\.\|\.transform\.\|subspecies.name" | head -10)
if [ -n "$NULL_CHAIN" ]; then
    echo "  ⚠️  发现可能的null引用链（a.b.c，建议加null检查）:"
    echo "$NULL_CHAIN"
    API_WARNINGS=$((API_WARNINGS+1))
fi

# 检查7: 创建外部对象后未验证关键字段
# 只检查PowerButtonCreator（可能返回null），不检查Object.Instantiate（参数非null则返回非null）
UNVERIFIED_CREATE=$(grep -rn "PowerButtonCreator\." "$MOD_DIR/Code" --include="*.cs" 2>/dev/null | grep -v "//" | while read line; do
    file=$(echo "$line" | cut -d: -f1)
    linenum=$(echo "$line" | cut -d: -f2)
    nextline=$((linenum+1))
    nextcontent=$(sed -n "${nextline}p" "$file" 2>/dev/null)
    if echo "$nextcontent" | grep -q "if.*!= null\|if.*== null\|VerifyButtonFields\|try"; then
        continue
    fi
    echo "$line"
done | head -10)
if [ -n "$UNVERIFIED_CREATE" ]; then
    echo "  ⚠️  发现创建外部对象后未验证（建议创建后检查关键字段）:"
    echo "$UNVERIFIED_CREATE"
    API_WARNINGS=$((API_WARNINGS+1))
fi

if [ "$API_WARNINGS" -eq 0 ]; then
    echo "  ✅ API使用模式检查通过"
fi

echo ""
echo "[附加检查] 本地化/资源/配置/Harmony..."

# 检查8: 本地化完整性 - 扫描所有getText调用，验证cz.json中有对应key
LOCALE_WARNINGS=0
export MOD_DIR
MISSING_LOCALE=$(python3 << 'PYEOF'
import json, re, os

mod_dir = os.environ.get('MOD_DIR', '.')
with open(f'{mod_dir}/Locales/cz.json', 'r', encoding='utf-8') as f:
    locale = json.load(f)

# 扫描所有C#文件中的getText调用
missing = set()
for root, dirs, files in os.walk(f'{mod_dir}/Code'):
    for f in files:
        if not f.endswith('.cs'): continue
        with open(os.path.join(root, f), 'r', encoding='utf-8') as fh:
            content = fh.read()
        # 匹配 LocalizedTextManager.getText("xxx") 或 getText("xxx")
        # 排除动态拼接的key（比如 "sm_icon_" + name）
        for m in re.finditer(r'getText\s*\(\s*"([^"]+)"\s*\)', content):
            key = m.group(1)
            # 排除明显是前缀的key（太短或以下划线结尾）
            if len(key) < 5 or key.endswith('_'):
                continue
            if key not in locale:
                missing.add(key)

for key in sorted(missing):
    print(key)
PYEOF
)
if [ -n "$MISSING_LOCALE" ]; then
    echo "  ⚠️  发现本地化缺失key（cz.json中找不到）:"
    echo "$MISSING_LOCALE" | head -10
    LOCALE_WARNINGS=$(echo "$MISSING_LOCALE" | wc -l)
else
    echo "  ✅ 本地化完整性检查通过"
fi

# 检查9: 资源存在性 - 扫描所有Resources.Load调用，验证GameResources中有对应文件
RESOURCE_WARNINGS=0
MISSING_RESOURCE=$(python3 << 'PYEOF'
import re, os

mod_dir = os.environ.get('MOD_DIR', '.')
res_dir = f'{mod_dir}/GameResources'

# 获取所有资源文件（不含扩展名）
existing = set()
if os.path.exists(res_dir):
    for f in os.listdir(res_dir):
        name = os.path.splitext(f)[0]
        existing.add(name)

# 扫描所有C#文件中的Resources.Load调用
missing = set()
for root, dirs, files in os.walk(f'{mod_dir}/Code'):
    for f in files:
        if not f.endswith('.cs'): continue
        with open(os.path.join(root, f), 'r', encoding='utf-8') as fh:
            content = fh.read()
        # 匹配 Resources.Load<Sprite>("xxx") 或 Resources.Load("xxx")
        for m in re.finditer(r'Resources\.Load(?:<[^>]+>)?\s*\(\s*"([^"]+)"', content):
            res = m.group(1)
            if res not in existing and not res.startswith('actor_traits/') and not res.startswith('icon'):
                # 排除原版资源路径
                if '/' not in res:
                    missing.add(res)

for key in sorted(missing):
    print(key)
PYEOF
)
if [ -n "$MISSING_RESOURCE" ]; then
    echo "  ⚠️  发现缺失资源文件（GameResources中找不到）:"
    echo "$MISSING_RESOURCE" | head -10
    RESOURCE_WARNINGS=$(echo "$MISSING_RESOURCE" | wc -l)
else
    echo "  ✅ 资源存在性检查通过"
fi

# 检查10: mod.json格式 - 验证必填字段
CONFIG_WARNINGS=0
python3 << PYEOF
import json, os, sys, re

mod_dir = os.environ.get('MOD_DIR', '.')
try:
    with open(f'{mod_dir}/mod.json', 'r', encoding='utf-8') as f:
        mod = json.load(f)
except Exception as e:
    print(f"  ❌ mod.json解析失败: {e}")
    sys.exit(1)

required = ['name', 'version', 'author', 'entryPoint']
missing = [k for k in required if k not in mod]
if missing:
    print(f"  ⚠️  mod.json缺少必填字段: {missing}")
else:
    print("  ✅ mod.json格式检查通过")

# 检查版本号格式
version = mod.get('version', '')
if not re.match(r'^\d+\.\d+\.\d+-(alpha|beta|release)$', version):
    print(f"  ⚠️  版本号格式异常: {version}（应为 x.y.z-alpha/beta/release）")
PYEOF

# 检查11: Harmony补丁目标 - 验证补丁目标类/方法存在于反编译文件
HARMONY_WARNINGS=0
MISSING_HARMONY=$(python3 << 'PYEOF'
import re, os, glob

mod_dir = os.environ.get('MOD_DIR', '.')
ref_dir = '/home/user/Doubao/chats/38443538092133890/gz_ref'

# 扫描所有HarmonyPatch
patches = []
for root, dirs, files in os.walk(f'{mod_dir}/Code'):
    for f in files:
        if not f.endswith('.cs'): continue
        with open(os.path.join(root, f), 'r', encoding='utf-8') as fh:
            content = fh.read()
        # 匹配 [HarmonyPatch(typeof(ClassName), "MethodName")]
        for m in re.finditer(r'HarmonyPatch\s*\(\s*typeof\s*\(\s*(\w+)\s*\)\s*,\s*"(\w+)"', content):
            patches.append((m.group(1), m.group(2)))

# 检查反编译文件中是否有对应类和方法
missing = []
for cls, method in patches:
    # 查找类文件
    class_files = glob.glob(f'{ref_dir}/**/{cls}.cs', recursive=True)
    if not class_files:
        missing.append(f"{cls}.{method} (类不存在)")
        continue
    # 检查方法是否存在
    found = False
    for cf in class_files:
        with open(cf, 'r', encoding='utf-8', errors='ignore') as fh:
            if f' {method}(' in fh.read():
                found = True
                break
    if not found:
        missing.append(f"{cls}.{method} (方法不存在)")

for m in missing:
    print(m)
PYEOF
)
if [ -n "$MISSING_HARMONY" ]; then
    echo "  ⚠️  发现Harmony补丁目标不存在:"
    echo "$MISSING_HARMONY" | head -10
    HARMONY_WARNINGS=$(echo "$MISSING_HARMONY" | wc -l)
else
    echo "  ✅ Harmony补丁目标检查通过"
fi

echo ""
echo "=== 检查完成 ==="
echo "代码错误: $ERROR_COUNT | 括号错误: $BRACKET_ERRORS | API警告: $API_WARNINGS | CS1069环境警告: $CS1069_COUNT"
echo "本地化缺失: $LOCALE_WARNINGS | 资源缺失: $RESOURCE_WARNINGS | Harmony问题: $HARMONY_WARNINGS"
if [ "$ERROR_COUNT" -eq 0 ] && [ "$BRACKET_ERRORS" -eq 0 ]; then
    echo "✅ 可以打包发布"
else
    echo "❌ 请修复上述问题后再打包"
fi
