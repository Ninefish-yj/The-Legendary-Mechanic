using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 超A名号生成器。原著ch770：种族名用超A自己的名号。
    /// 名号 = 前缀 + 后缀，按职业系有不同风格词库。
    /// 包含原著出现过的名号（黑星/霸者/起誓人等）和大量自创词。
    /// </summary>
    public static class SuperMechTitleGenerator
    {
        // 机械系词库
        private static readonly string[] MechPrefix = {
            "sm_title_mech_pre_000","sm_title_mech_pre_001","sm_title_mech_pre_002","sm_title_mech_pre_077","sm_title_mech_pre_004","sm_title_mech_pre_005","sm_title_mech_pre_006","sm_title_mech_pre_007","sm_title_mech_pre_008","sm_title_mech_pre_009","sm_title_mech_pre_010","sm_title_mech_pre_011","sm_title_mech_pre_012","sm_title_mech_pre_013","sm_title_mech_pre_014",
            "sm_title_mech_pre_015","sm_title_mech_pre_016","sm_title_mech_pre_017","sm_title_mech_pre_018","sm_title_mech_pre_019","sm_title_mech_pre_020","sm_title_mech_pre_021","sm_title_mech_pre_022","sm_title_mech_pre_023","sm_title_mech_pre_024","sm_title_mech_pre_025","sm_title_mech_pre_026","sm_title_mech_pre_027","sm_title_mech_pre_028",
            "sm_title_mech_pre_029","sm_title_mech_pre_030","sm_title_mech_pre_031","sm_title_mech_pre_032","sm_title_mech_pre_033","sm_title_mech_pre_034","sm_title_mech_pre_035","sm_title_mech_pre_036","sm_title_mech_pre_037","sm_title_mech_pre_038","sm_title_mech_pre_039","sm_title_mech_pre_040",
            "sm_title_mech_pre_041","sm_title_mech_pre_042","sm_title_mech_pre_043","sm_title_mech_pre_044","sm_title_mech_pre_045","sm_title_mech_pre_046","sm_title_mech_pre_047","sm_title_mech_pre_048","sm_title_mech_pre_049","sm_title_mech_pre_050","sm_title_mech_pre_051",
            "sm_title_mech_pre_052","sm_title_mech_pre_053","sm_title_mech_pre_054","sm_title_mech_pre_055","sm_title_mech_pre_056","sm_title_mech_pre_057","sm_title_mech_pre_058","sm_title_mech_pre_059","sm_title_mech_pre_060","sm_title_mech_pre_061","sm_title_mech_pre_062","sm_title_mech_pre_063","sm_title_mech_pre_064","sm_title_mech_pre_065","sm_title_mech_pre_066",
            "sm_title_mech_pre_067","sm_title_mech_pre_068","sm_title_mech_pre_069","sm_title_mech_pre_070","sm_title_mech_pre_071","sm_title_mech_pre_072","sm_title_mech_pre_073","sm_title_mech_pre_074","sm_title_mech_pre_075","sm_title_mech_pre_076","sm_title_mech_pre_077","sm_title_mech_pre_078","sm_title_mech_pre_079","sm_title_mech_pre_080","sm_title_mech_pre_081","sm_title_mech_pre_082"
        };
        private static readonly string[] MechSuffix = {
            "sm_title_mech_suf_045","sm_title_mech_suf_001","sm_title_mech_suf_002","sm_title_mech_suf_003","sm_title_mech_suf_004","sm_title_mech_suf_005","sm_title_mech_suf_006","sm_title_mech_suf_007","sm_title_mech_suf_008","sm_title_mech_suf_009","sm_title_mech_suf_010","sm_title_mech_suf_011","sm_title_mech_suf_012","sm_title_mech_suf_013","sm_title_mech_suf_014",
            "sm_title_mech_suf_015","sm_title_mech_suf_016","sm_title_mech_suf_017","sm_title_mech_suf_018","sm_title_mech_suf_019","sm_title_mech_suf_020","sm_title_mech_suf_021","sm_title_mech_suf_022","sm_title_mech_suf_023","sm_title_mech_suf_024","sm_title_mech_suf_025","sm_title_mech_suf_026","sm_title_mech_suf_027","sm_title_mech_suf_028","sm_title_mech_suf_029",
            "sm_title_mech_suf_030","sm_title_mech_suf_031","sm_title_mech_suf_032","sm_title_mech_suf_033","sm_title_mech_suf_034","sm_title_mech_suf_035","sm_title_mech_suf_036","sm_title_mech_suf_037","sm_title_mech_suf_038","sm_title_mech_suf_039","sm_title_mech_suf_040","sm_title_mech_suf_041","sm_title_mech_suf_042","sm_title_mech_suf_043","sm_title_mech_suf_044",
            "sm_title_mech_suf_045","sm_title_mech_suf_046","sm_title_mech_suf_047","sm_title_mech_suf_048","sm_title_mech_suf_049","sm_title_mech_suf_050","sm_title_mech_suf_051","sm_title_mech_suf_052","sm_title_mech_suf_053","sm_title_mech_suf_054","sm_title_mech_suf_055","sm_title_mech_suf_056","sm_title_mech_suf_057","sm_title_mech_suf_058","sm_title_mech_suf_059",
            "sm_title_mech_suf_060","sm_title_mech_suf_061","sm_title_mech_suf_062","sm_title_mech_suf_063","sm_title_mech_suf_064","sm_title_mech_suf_065","sm_title_mech_suf_066","sm_title_mech_suf_067",
            "sm_title_mech_suf_068","sm_title_mech_suf_069","sm_title_mech_suf_070","sm_title_mech_suf_071","sm_title_mech_suf_072","sm_title_mech_suf_073","sm_title_mech_suf_074","sm_title_mech_suf_075","sm_title_mech_suf_076","sm_title_mech_suf_077",
            "sm_title_mech_suf_078","sm_title_mech_suf_079","sm_title_mech_suf_080","sm_title_mech_suf_081","sm_title_mech_suf_082","sm_title_mech_suf_083","sm_title_mech_suf_084","sm_title_mech_suf_085",
            "sm_title_mech_suf_086","sm_title_mech_suf_087","sm_title_mech_suf_088","sm_title_mech_suf_089","sm_title_mech_suf_090","sm_title_mech_suf_091","sm_title_mech_suf_092","sm_title_mech_suf_093"
        };

        // 武道系词库（科幻风格：基因/躯体/战斗/能级）
        private static readonly string[] MartialPrefix = {
            "sm_title_martial_pre_000","sm_title_martial_pre_001","sm_title_martial_pre_002","sm_title_martial_pre_003","sm_title_martial_pre_004","sm_title_martial_pre_005","sm_title_martial_pre_006","sm_title_martial_pre_007","sm_title_martial_pre_008","sm_title_martial_pre_009","sm_title_martial_pre_010","sm_title_martial_pre_011","sm_title_martial_pre_012","sm_title_martial_pre_013",
            "sm_title_martial_pre_014","sm_title_martial_pre_015","sm_title_martial_pre_016","sm_title_martial_pre_017","sm_title_martial_pre_018","sm_title_martial_pre_019","sm_title_martial_pre_020","sm_title_martial_pre_021","sm_title_martial_pre_022","sm_title_martial_pre_023","sm_title_martial_pre_024","sm_title_martial_pre_025","sm_title_martial_pre_026","sm_title_martial_pre_027",
            "sm_title_martial_pre_028","sm_title_martial_pre_029","sm_title_martial_pre_030","sm_title_martial_pre_031","sm_title_martial_pre_032","sm_title_martial_pre_033","sm_title_martial_pre_034","sm_title_martial_pre_035","sm_title_martial_pre_036","sm_title_martial_pre_037","sm_title_martial_pre_038","sm_title_martial_pre_039","sm_title_martial_pre_089","sm_title_martial_pre_041","sm_title_martial_pre_042",
            "sm_title_martial_pre_043","sm_title_martial_pre_044","sm_title_martial_pre_045","sm_title_martial_pre_046","sm_title_martial_pre_047","sm_title_martial_pre_048","sm_title_martial_pre_049","sm_title_martial_pre_050","sm_title_martial_pre_051","sm_title_martial_pre_052","sm_title_martial_pre_053","sm_title_martial_pre_054","sm_title_martial_pre_055","sm_title_martial_pre_056","sm_title_martial_pre_057",
            "sm_title_martial_pre_058","sm_title_martial_pre_059","sm_title_martial_pre_060","sm_title_martial_pre_061","sm_title_martial_pre_062","sm_title_martial_pre_063","sm_title_martial_pre_064","sm_title_martial_pre_065","sm_title_martial_pre_066","sm_title_martial_pre_067","sm_title_martial_pre_068","sm_title_martial_pre_069","sm_title_martial_pre_070","sm_title_martial_pre_071","sm_title_martial_pre_072",
            "sm_title_martial_pre_073","sm_title_martial_pre_074","sm_title_martial_pre_075","sm_title_martial_pre_076","sm_title_martial_pre_077","sm_title_martial_pre_078","sm_title_martial_pre_079","sm_title_martial_pre_080","sm_title_martial_pre_081","sm_title_martial_pre_082","sm_title_martial_pre_083","sm_title_martial_pre_084","sm_title_martial_pre_085","sm_title_martial_pre_086","sm_title_martial_pre_087",
            "sm_title_martial_pre_088","sm_title_martial_pre_089","sm_title_martial_pre_090","sm_title_martial_pre_091","sm_title_martial_pre_092","sm_title_martial_pre_093","sm_title_martial_pre_094","sm_title_martial_pre_095","sm_title_martial_pre_096","sm_title_martial_pre_097","sm_title_martial_pre_098","sm_title_martial_pre_099","sm_title_martial_pre_100","sm_title_martial_pre_101","sm_title_martial_pre_102"
        };
        private static readonly string[] MartialSuffix = {
            "sm_title_martial_suf_000","sm_title_martial_suf_001","sm_title_martial_suf_002","sm_title_martial_suf_003","sm_title_martial_suf_004","sm_title_martial_suf_005","sm_title_martial_suf_006","sm_title_martial_suf_007","sm_title_martial_suf_008","sm_title_martial_suf_009","sm_title_martial_suf_010","sm_title_martial_suf_011","sm_title_martial_suf_012","sm_title_martial_suf_013","sm_title_martial_suf_014",
            "sm_title_martial_suf_015","sm_title_martial_suf_016","sm_title_martial_suf_017","sm_title_martial_suf_018","sm_title_martial_suf_019","sm_title_martial_suf_020","sm_title_martial_suf_021","sm_title_martial_suf_022","sm_title_martial_suf_023","sm_title_martial_suf_024","sm_title_martial_suf_025","sm_title_martial_suf_026","sm_title_martial_suf_027","sm_title_martial_suf_028","sm_title_martial_suf_029",
            "sm_title_martial_suf_030","sm_title_martial_suf_031","sm_title_martial_suf_032","sm_title_martial_suf_033","sm_title_martial_suf_034","sm_title_martial_suf_035","sm_title_martial_suf_036","sm_title_martial_suf_037","sm_title_martial_suf_038","sm_title_martial_suf_039",
            "sm_title_martial_suf_040","sm_title_martial_suf_041","sm_title_martial_suf_042","sm_title_martial_suf_043","sm_title_martial_suf_044","sm_title_martial_suf_045","sm_title_martial_suf_046","sm_title_martial_suf_047","sm_title_martial_suf_048","sm_title_martial_suf_049",
            "sm_title_martial_suf_050","sm_title_martial_suf_051","sm_title_martial_suf_052","sm_title_martial_suf_053","sm_title_martial_suf_054","sm_title_martial_suf_055","sm_title_martial_suf_056","sm_title_martial_suf_057","sm_title_martial_suf_058","sm_title_martial_suf_059",
            "sm_title_martial_suf_060","sm_title_martial_suf_061","sm_title_martial_suf_062","sm_title_martial_suf_063","sm_title_martial_suf_064","sm_title_martial_suf_065","sm_title_martial_suf_066","sm_title_martial_suf_067","sm_title_martial_suf_068","sm_title_martial_suf_069",
            "sm_title_martial_suf_070","sm_title_martial_suf_071","sm_title_martial_suf_072","sm_title_martial_suf_073","sm_title_martial_suf_074","sm_title_martial_suf_075","sm_title_martial_suf_076","sm_title_martial_suf_077","sm_title_martial_suf_078","sm_title_martial_suf_079",
            "sm_title_martial_suf_080","sm_title_martial_suf_081","sm_title_martial_suf_082","sm_title_martial_suf_083","sm_title_martial_suf_084","sm_title_martial_suf_085","sm_title_martial_suf_086","sm_title_martial_suf_087","sm_title_martial_suf_088","sm_title_martial_suf_089",
            "sm_title_martial_suf_090","sm_title_martial_suf_091","sm_title_martial_suf_092","sm_title_martial_suf_093","sm_title_martial_suf_094","sm_title_martial_suf_095","sm_title_martial_suf_096","sm_title_martial_suf_097","sm_title_martial_suf_098","sm_title_martial_suf_099",
            "sm_title_martial_suf_100","sm_title_martial_suf_101","sm_title_martial_suf_102","sm_title_martial_suf_103","sm_title_martial_suf_104","sm_title_martial_suf_105","sm_title_martial_suf_106"
        };

        // 异能系词库
        private static readonly string[] PsiPrefix = {
            "sm_title_psi_pre_000","sm_title_psi_pre_001","sm_title_psi_pre_002","sm_title_psi_pre_003","sm_title_psi_pre_004","sm_title_psi_pre_005","sm_title_psi_pre_006","sm_title_psi_pre_007","sm_title_psi_pre_008","sm_title_psi_pre_009","sm_title_psi_pre_010","sm_title_psi_pre_052","sm_title_psi_pre_012","sm_title_psi_pre_013","sm_title_psi_pre_014",
            "sm_title_psi_pre_015","sm_title_psi_pre_016","sm_title_psi_pre_017","sm_title_psi_pre_018","sm_title_psi_pre_019","sm_title_psi_pre_020","sm_title_psi_pre_021","sm_title_psi_pre_022","sm_title_psi_pre_023","sm_title_psi_pre_024","sm_title_psi_pre_075","sm_title_psi_pre_026","sm_title_psi_pre_027","sm_title_psi_pre_073","sm_title_psi_pre_029",
            "sm_title_psi_pre_030","sm_title_psi_pre_031","sm_title_psi_pre_032","sm_title_psi_pre_074","sm_title_psi_pre_034","sm_title_psi_pre_035","sm_title_psi_pre_036","sm_title_psi_pre_037","sm_title_psi_pre_038","sm_title_psi_pre_039","sm_title_psi_pre_040","sm_title_psi_pre_041","sm_title_psi_pre_042","sm_title_psi_pre_043","sm_title_psi_pre_044",
            "sm_title_psi_pre_045","sm_title_psi_pre_046","sm_title_psi_pre_047","sm_title_psi_pre_048","sm_title_psi_pre_049","sm_title_psi_pre_050","sm_title_psi_pre_051","sm_title_psi_pre_052","sm_title_psi_pre_053","sm_title_psi_pre_054","sm_title_psi_pre_055","sm_title_psi_pre_056","sm_title_psi_pre_057","sm_title_psi_pre_058","sm_title_psi_pre_059",
            "sm_title_psi_pre_060","sm_title_psi_pre_061","sm_title_psi_pre_062","sm_title_psi_pre_063","sm_title_psi_pre_064","sm_title_psi_pre_065","sm_title_psi_pre_066","sm_title_psi_pre_067","sm_title_psi_pre_068","sm_title_psi_pre_069","sm_title_psi_pre_070","sm_title_psi_pre_071","sm_title_psi_pre_072","sm_title_psi_pre_073","sm_title_psi_pre_074",
            "sm_title_psi_pre_075","sm_title_psi_pre_076","sm_title_psi_pre_077","sm_title_psi_pre_078","sm_title_psi_pre_079","sm_title_psi_pre_080","sm_title_psi_pre_081","sm_title_psi_pre_082","sm_title_psi_pre_083","sm_title_psi_pre_084","sm_title_psi_pre_085","sm_title_psi_pre_086","sm_title_psi_pre_087","sm_title_psi_pre_088","sm_title_psi_pre_089"
        };
        private static readonly string[] PsiSuffix = {
            "sm_title_psi_suf_000","sm_title_psi_suf_001","sm_title_psi_suf_002","sm_title_psi_suf_003","sm_title_psi_suf_004","sm_title_psi_suf_005","sm_title_psi_suf_006","sm_title_psi_suf_007","sm_title_psi_suf_008","sm_title_psi_suf_009","sm_title_psi_suf_010","sm_title_psi_suf_011","sm_title_psi_suf_012","sm_title_psi_suf_013","sm_title_psi_suf_014",
            "sm_title_psi_suf_015","sm_title_psi_suf_016","sm_title_psi_suf_017","sm_title_psi_suf_018","sm_title_psi_suf_019","sm_title_psi_suf_020","sm_title_psi_suf_021","sm_title_psi_suf_022","sm_title_psi_suf_023","sm_title_psi_suf_024","sm_title_psi_suf_025","sm_title_psi_suf_026","sm_title_psi_suf_027","sm_title_psi_suf_028","sm_title_psi_suf_029",
            "sm_title_psi_suf_030","sm_title_psi_suf_031","sm_title_psi_suf_032","sm_title_psi_suf_033","sm_title_psi_suf_034","sm_title_psi_suf_035","sm_title_psi_suf_036","sm_title_psi_suf_037",
            "sm_title_psi_suf_038","sm_title_psi_suf_039","sm_title_psi_suf_040","sm_title_psi_suf_041","sm_title_psi_suf_042","sm_title_psi_suf_043","sm_title_psi_suf_044","sm_title_psi_suf_045",
            "sm_title_psi_suf_046","sm_title_psi_suf_047","sm_title_psi_suf_048","sm_title_psi_suf_049","sm_title_psi_suf_050","sm_title_psi_suf_051",
            "sm_title_psi_suf_052","sm_title_psi_suf_053","sm_title_psi_suf_054","sm_title_psi_suf_055","sm_title_psi_suf_056","sm_title_psi_suf_057",
            "sm_title_psi_suf_058","sm_title_psi_suf_059","sm_title_psi_suf_060","sm_title_psi_suf_061","sm_title_psi_suf_062","sm_title_psi_suf_063","sm_title_psi_suf_064","sm_title_psi_suf_065",
            "sm_title_psi_suf_066","sm_title_psi_suf_067","sm_title_psi_suf_068","sm_title_psi_suf_069","sm_title_psi_suf_070","sm_title_psi_suf_071","sm_title_psi_suf_072","sm_title_psi_suf_073","sm_title_psi_suf_074","sm_title_psi_suf_075",
            "sm_title_psi_suf_076","sm_title_psi_suf_077","sm_title_psi_suf_078","sm_title_psi_suf_079","sm_title_psi_suf_080","sm_title_psi_suf_081","sm_title_psi_suf_082","sm_title_psi_suf_083","sm_title_psi_suf_084","sm_title_psi_suf_085",
            "sm_title_psi_suf_086","sm_title_psi_suf_087","sm_title_psi_suf_088","sm_title_psi_suf_089","sm_title_psi_suf_090","sm_title_psi_suf_091","sm_title_psi_suf_092","sm_title_psi_suf_093","sm_title_psi_suf_094","sm_title_psi_suf_095"
        };

        // 魔法系词库（科幻风格：魔网/符文/能级/编码）
        private static readonly string[] MagePrefix = {
            "sm_title_mage_pre_000","sm_title_mage_pre_001","sm_title_mage_pre_002","sm_title_mage_pre_003","sm_title_mage_pre_004","sm_title_mage_pre_005","sm_title_mage_pre_006","sm_title_mage_pre_007","sm_title_mage_pre_008","sm_title_mage_pre_009","sm_title_mage_pre_010","sm_title_mage_pre_011","sm_title_mage_pre_012","sm_title_mage_pre_013","sm_title_mage_pre_014",
            "sm_title_mage_pre_065","sm_title_mage_pre_016","sm_title_mage_pre_017","sm_title_mage_pre_018","sm_title_mage_pre_019","sm_title_mage_pre_020","sm_title_mage_pre_021","sm_title_mage_pre_022","sm_title_mage_pre_023","sm_title_mage_pre_024","sm_title_mage_pre_025","sm_title_mage_pre_026","sm_title_mage_pre_027","sm_title_mage_pre_028","sm_title_mage_pre_029",
            "sm_title_mage_pre_030","sm_title_mage_pre_031","sm_title_mage_pre_032","sm_title_mage_pre_033","sm_title_mage_pre_034","sm_title_mage_pre_035","sm_title_mage_pre_036","sm_title_mage_pre_037","sm_title_mage_pre_038","sm_title_mage_pre_039","sm_title_mage_pre_040","sm_title_mage_pre_041","sm_title_mage_pre_042","sm_title_mage_pre_043","sm_title_mage_pre_044",
            "sm_title_mage_pre_045","sm_title_mage_pre_046","sm_title_mage_pre_047","sm_title_mage_pre_048","sm_title_mage_pre_049","sm_title_mage_pre_050","sm_title_mage_pre_051","sm_title_mage_pre_052","sm_title_mage_pre_053","sm_title_mage_pre_054","sm_title_mage_pre_055","sm_title_mage_pre_056","sm_title_mage_pre_057","sm_title_mage_pre_058","sm_title_mage_pre_059",
            "sm_title_mage_pre_060","sm_title_mage_pre_061","sm_title_mage_pre_103","sm_title_mage_pre_063","sm_title_mage_pre_064","sm_title_mage_pre_065","sm_title_mage_pre_066","sm_title_mage_pre_067","sm_title_mage_pre_068","sm_title_mage_pre_069","sm_title_mage_pre_070","sm_title_mage_pre_071","sm_title_mage_pre_072","sm_title_mage_pre_073","sm_title_mage_pre_074",
            "sm_title_mage_pre_075","sm_title_mage_pre_076","sm_title_mage_pre_077","sm_title_mage_pre_078","sm_title_mage_pre_079","sm_title_mage_pre_080","sm_title_mage_pre_081","sm_title_mage_pre_082","sm_title_mage_pre_083","sm_title_mage_pre_084","sm_title_mage_pre_085","sm_title_mage_pre_086","sm_title_mage_pre_087","sm_title_mage_pre_088","sm_title_mage_pre_089",
            "sm_title_mage_pre_090","sm_title_mage_pre_091","sm_title_mage_pre_092","sm_title_mage_pre_093","sm_title_mage_pre_094","sm_title_mage_pre_095","sm_title_mage_pre_096","sm_title_mage_pre_097","sm_title_mage_pre_098","sm_title_mage_pre_099","sm_title_mage_pre_100","sm_title_mage_pre_101","sm_title_mage_pre_102","sm_title_mage_pre_103","sm_title_mage_pre_104"
        };
        private static readonly string[] MageSuffix = {
            "sm_title_mage_suf_000","sm_title_mage_suf_001","sm_title_mage_suf_002","sm_title_mage_suf_003","sm_title_mage_suf_004","sm_title_mage_suf_005","sm_title_mage_suf_006","sm_title_mage_suf_007","sm_title_mage_suf_008","sm_title_mage_suf_009","sm_title_mage_suf_010","sm_title_mage_suf_011","sm_title_mage_suf_012","sm_title_mage_suf_013","sm_title_mage_suf_014",
            "sm_title_mage_suf_015","sm_title_mage_suf_016","sm_title_mage_suf_017","sm_title_mage_suf_018","sm_title_mage_suf_019","sm_title_mage_suf_020","sm_title_mage_suf_021","sm_title_mage_suf_022","sm_title_mage_suf_023","sm_title_mage_suf_024","sm_title_mage_suf_025","sm_title_mage_suf_026","sm_title_mage_suf_027","sm_title_mage_suf_028","sm_title_mage_suf_029",
            "sm_title_mage_suf_030","sm_title_mage_suf_031","sm_title_mage_suf_032","sm_title_mage_suf_033","sm_title_mage_suf_034","sm_title_mage_suf_035","sm_title_mage_suf_036","sm_title_mage_suf_037","sm_title_mage_suf_038","sm_title_mage_suf_039",
            "sm_title_mage_suf_040","sm_title_mage_suf_041","sm_title_mage_suf_042","sm_title_mage_suf_043","sm_title_mage_suf_044","sm_title_mage_suf_045","sm_title_mage_suf_046","sm_title_mage_suf_047","sm_title_mage_suf_048","sm_title_mage_suf_049",
            "sm_title_mage_suf_050","sm_title_mage_suf_051","sm_title_mage_suf_052","sm_title_mage_suf_053","sm_title_mage_suf_054","sm_title_mage_suf_055","sm_title_mage_suf_056","sm_title_mage_suf_057",
            "sm_title_mage_suf_058","sm_title_mage_suf_059","sm_title_mage_suf_060","sm_title_mage_suf_061","sm_title_mage_suf_062","sm_title_mage_suf_063","sm_title_mage_suf_064","sm_title_mage_suf_065",
            "sm_title_mage_suf_066","sm_title_mage_suf_067","sm_title_mage_suf_068","sm_title_mage_suf_069","sm_title_mage_suf_070","sm_title_mage_suf_071","sm_title_mage_suf_072","sm_title_mage_suf_073","sm_title_mage_suf_074","sm_title_mage_suf_075",
            "sm_title_mage_suf_076","sm_title_mage_suf_077","sm_title_mage_suf_078","sm_title_mage_suf_079","sm_title_mage_suf_080","sm_title_mage_suf_081","sm_title_mage_suf_082","sm_title_mage_suf_083","sm_title_mage_suf_084","sm_title_mage_suf_085",
            "sm_title_mage_suf_086","sm_title_mage_suf_087","sm_title_mage_suf_088","sm_title_mage_suf_089","sm_title_mage_suf_090","sm_title_mage_suf_091","sm_title_mage_suf_092","sm_title_mage_suf_093","sm_title_mage_suf_094","sm_title_mage_suf_095",
            "sm_title_mage_suf_096","sm_title_mage_suf_097","sm_title_mage_suf_098","sm_title_mage_suf_099","sm_title_mage_suf_100","sm_title_mage_suf_101","sm_title_mage_suf_102","sm_title_mage_suf_103","sm_title_mage_suf_104","sm_title_mage_suf_105",
            "sm_title_mage_suf_106","sm_title_mage_suf_107","sm_title_mage_suf_108","sm_title_mage_suf_109","sm_title_mage_suf_110","sm_title_mage_suf_111","sm_title_mage_suf_112","sm_title_mage_suf_113","sm_title_mage_suf_114","sm_title_mage_suf_115"
        };

        // 念力系词库（科幻风格：精神/意识/脑域/信息态/量子）
        private static readonly string[] MindPrefix = {
            "sm_title_mind_pre_000","sm_title_mind_pre_001","sm_title_mind_pre_002","sm_title_mind_pre_003","sm_title_mind_pre_004","sm_title_mind_pre_005","sm_title_mind_pre_006","sm_title_mind_pre_007","sm_title_mind_pre_008","sm_title_mind_pre_009","sm_title_mind_pre_010","sm_title_mind_pre_011","sm_title_mind_pre_012","sm_title_mind_pre_013","sm_title_mind_pre_014",
            "sm_title_mind_pre_015","sm_title_mind_pre_058","sm_title_mind_pre_063","sm_title_mind_pre_018","sm_title_mind_pre_019","sm_title_mind_pre_020","sm_title_mind_pre_021","sm_title_mind_pre_022","sm_title_mind_pre_023","sm_title_mind_pre_024","sm_title_mind_pre_025","sm_title_mind_pre_026","sm_title_mind_pre_027","sm_title_mind_pre_028","sm_title_mind_pre_029",
            "sm_title_mind_pre_030","sm_title_mind_pre_031","sm_title_mind_pre_032","sm_title_mind_pre_033","sm_title_mind_pre_034","sm_title_mind_pre_058","sm_title_mind_pre_036","sm_title_mind_pre_037","sm_title_mind_pre_038","sm_title_mind_pre_039","sm_title_mind_pre_040","sm_title_mind_pre_041","sm_title_mind_pre_042","sm_title_mind_pre_043","sm_title_mind_pre_044",
            "sm_title_mind_pre_045","sm_title_mind_pre_046","sm_title_mind_pre_047","sm_title_mind_pre_048","sm_title_mind_pre_049","sm_title_mind_pre_050","sm_title_mind_pre_051","sm_title_mind_pre_052","sm_title_mind_pre_053","sm_title_mind_pre_054","sm_title_mind_pre_055","sm_title_mind_pre_056","sm_title_mind_pre_063","sm_title_mind_pre_058","sm_title_mind_pre_063",
            "sm_title_mind_pre_060","sm_title_mind_pre_061","sm_title_mind_pre_062","sm_title_mind_pre_063","sm_title_mind_pre_064","sm_title_mind_pre_065","sm_title_mind_pre_066","sm_title_mind_pre_067","sm_title_mind_pre_068","sm_title_mind_pre_069","sm_title_mind_pre_070","sm_title_mind_pre_071","sm_title_mind_pre_072","sm_title_mind_pre_073","sm_title_mind_pre_074",
            "sm_title_mind_pre_075","sm_title_mind_pre_076","sm_title_mind_pre_077","sm_title_mind_pre_078","sm_title_mind_pre_079","sm_title_mind_pre_080","sm_title_mind_pre_081","sm_title_mind_pre_082","sm_title_mind_pre_083","sm_title_mind_pre_084","sm_title_mind_pre_085","sm_title_mind_pre_086","sm_title_mind_pre_087","sm_title_mind_pre_088","sm_title_mind_pre_089",
            "sm_title_mind_pre_092","sm_title_mind_pre_091","sm_title_mind_pre_092","sm_title_mind_pre_093","sm_title_mind_pre_094","sm_title_mind_pre_095","sm_title_mind_pre_096","sm_title_mind_pre_097","sm_title_mind_pre_098","sm_title_mind_pre_099","sm_title_mind_pre_100","sm_title_mind_pre_101","sm_title_mind_pre_102","sm_title_mind_pre_103","sm_title_mind_pre_104"
        };
        private static readonly string[] MindSuffix = {
            "sm_title_mind_suf_000","sm_title_mind_suf_001","sm_title_mind_suf_002","sm_title_mind_suf_003","sm_title_mind_suf_004","sm_title_mind_suf_005","sm_title_mind_suf_006","sm_title_mind_suf_021","sm_title_mind_suf_017","sm_title_mind_suf_014","sm_title_mind_suf_010","sm_title_mind_suf_011","sm_title_mind_suf_012","sm_title_mind_suf_013","sm_title_mind_suf_014",
            "sm_title_mind_suf_015","sm_title_mind_suf_016","sm_title_mind_suf_017","sm_title_mind_suf_018","sm_title_mind_suf_019","sm_title_mind_suf_020","sm_title_mind_suf_021","sm_title_mind_suf_022","sm_title_mind_suf_023","sm_title_mind_suf_024","sm_title_mind_suf_025","sm_title_mind_suf_026","sm_title_mind_suf_027","sm_title_mind_suf_028","sm_title_mind_suf_029",
            "sm_title_mind_suf_030","sm_title_mind_suf_031","sm_title_mind_suf_032","sm_title_mind_suf_033","sm_title_mind_suf_034","sm_title_mind_suf_035","sm_title_mind_suf_036","sm_title_mind_suf_037","sm_title_mind_suf_038","sm_title_mind_suf_039",
            "sm_title_mind_suf_040","sm_title_mind_suf_041","sm_title_mind_suf_042","sm_title_mind_suf_043","sm_title_mind_suf_044","sm_title_mind_suf_045","sm_title_mind_suf_046","sm_title_mind_suf_047","sm_title_mind_suf_048","sm_title_mind_suf_049",
            "sm_title_mind_suf_050","sm_title_mind_suf_051","sm_title_mind_suf_052","sm_title_mind_suf_053","sm_title_mind_suf_054","sm_title_mind_suf_055","sm_title_mind_suf_056","sm_title_mind_suf_057","sm_title_mind_suf_058","sm_title_mind_suf_059",
            "sm_title_mind_suf_060","sm_title_mind_suf_061","sm_title_mind_suf_062","sm_title_mind_suf_063","sm_title_mind_suf_064","sm_title_mind_suf_065","sm_title_mind_suf_066","sm_title_mind_suf_067","sm_title_mind_suf_068","sm_title_mind_suf_069",
            "sm_title_mind_suf_070","sm_title_mind_suf_071","sm_title_mind_suf_072","sm_title_mind_suf_073","sm_title_mind_suf_074","sm_title_mind_suf_075","sm_title_mind_suf_076","sm_title_mind_suf_077","sm_title_mind_suf_078","sm_title_mind_suf_079",
            "sm_title_mind_suf_080","sm_title_mind_suf_081","sm_title_mind_suf_082","sm_title_mind_suf_083","sm_title_mind_suf_084","sm_title_mind_suf_085","sm_title_mind_suf_086","sm_title_mind_suf_087","sm_title_mind_suf_088","sm_title_mind_suf_089",
            "sm_title_mind_suf_090","sm_title_mind_suf_091","sm_title_mind_suf_092","sm_title_mind_suf_093","sm_title_mind_suf_094","sm_title_mind_suf_095","sm_title_mind_suf_096","sm_title_mind_suf_097","sm_title_mind_suf_098","sm_title_mind_suf_099",
            "sm_title_mind_suf_100","sm_title_mind_suf_101","sm_title_mind_suf_102","sm_title_mind_suf_103","sm_title_mind_suf_104","sm_title_mind_suf_105","sm_title_mind_suf_106","sm_title_mind_suf_107","sm_title_mind_suf_108","sm_title_mind_suf_109",
            "sm_title_mind_suf_110","sm_title_mind_suf_111","sm_title_mind_suf_112","sm_title_mind_suf_113","sm_title_mind_suf_114","sm_title_mind_suf_115","sm_title_mind_suf_116","sm_title_mind_suf_117","sm_title_mind_suf_118","sm_title_mind_suf_119"
        };

        private static readonly System.Random _rng = new System.Random();

        /// <summary>按职业系生成名号。有概率直接出原著名号。</summary>
        public static string Generate(string classId)
        {
            string[] prefix, suffix;
            switch (classId)
            {
                case SuperMechTraits.ClassMech:    prefix = MechPrefix;    suffix = MechSuffix;    break;
                case SuperMechTraits.ClassMartial: prefix = MartialPrefix; suffix = MartialSuffix; break;
                case SuperMechTraits.ClassPsi:     prefix = PsiPrefix;     suffix = PsiSuffix;     break;
                case SuperMechTraits.ClassMage:    prefix = MagePrefix;    suffix = MageSuffix;    break;
                case SuperMechTraits.ClassMind:    prefix = MindPrefix;    suffix = MindSuffix;    break;
                default:                           prefix = MechPrefix;    suffix = MechSuffix;    break;
            }

            // 10%概率直接用后缀里的完整名号（含原著词）
            if (_rng.Next(10) == 0)
                return LocalizedTextManager.getText(suffix[_rng.Next(suffix.Length)]);

            // 正常组合：前缀+后缀（key比较，避免同一个key）
            string p = prefix[_rng.Next(prefix.Length)];
            string s = suffix[_rng.Next(suffix.Length)];
            if (p == s) s = suffix[(_rng.Next(suffix.Length) + 1) % suffix.Length];
            // 转换key为中文后组合
            return LocalizedTextManager.getText(p) + LocalizedTextManager.getText(s);
        }
    }
}
