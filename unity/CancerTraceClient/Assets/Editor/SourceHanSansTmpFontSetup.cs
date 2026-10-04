using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CancerTrace.EditorTools
{
    public static class SourceHanSansTmpFontSetup
    {
        private const string SourcePath = "Assets/Fonts/SourceHanSansSC-Regular.otf";
        private const string AssetPath = "Assets/Fonts/TMP/SourceHanSansSC-Regular SDF.asset";
        private const string TestText =
            "CancerTrace 0123456789\n" +
            "细胞侦探所\n科研点\n初始线索\n基因扫描\n癌症星图\nAI 科研助手\n证据板\n提交诊断\n异常案件\n" +
            "肺\n皮肤\n中枢神经 / 脑\n肠道\n食管 / 胃\n乳腺\n骨\n卵巢 / 输卵管\n正确\n错误\n继续调查\n开始调查\n" +
            "Gene Scan\nCancer Galaxy\nAI Assistant\nLung\nCNS/Brain\nAI 科研助手\nCancer Galaxy 癌症星图，。！？：；（）";

        [MenuItem("CancerTrace/Fonts/Create and Verify Source Han Sans TMP Font")]
        public static void CreateAndVerify()
        {
            AssetDatabase.ImportAsset(SourcePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
            Require(sourceFont != null, "Unity did not import the Source Han Sans OTF as a Font asset.");
            Require(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) == null,
                "TMP Font Asset already exists; refusing to overwrite it.");

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            Require(fontAsset != null, "TMP failed to create a Font Asset from Source Han Sans SC Regular.");

            fontAsset.name = "SourceHanSansSC-Regular SDF";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            fontAsset.atlasTextures[0].name = "SourceHanSansSC-Regular SDF Atlas";
            fontAsset.material.name = "SourceHanSansSC-Regular SDF Material";

            AssetDatabase.CreateAsset(fontAsset, AssetPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            string missingDuringAdd;
            bool added = fontAsset.TryAddCharacters(TestText, out missingDuringAdd, true);
            Require(added && string.IsNullOrEmpty(missingDuringAdd),
                "Missing glyphs while populating verification text: " + missingDuringAdd);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            TMP_FontAsset reloaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            Require(reloaded != null, "Saved TMP Font Asset could not be reloaded.");
            Require(reloaded.atlasPopulationMode == AtlasPopulationMode.Dynamic,
                "Atlas Population Mode is not Dynamic.");
            Require(reloaded.isMultiAtlasTexturesEnabled,
                "Multi Atlas Textures is not enabled.");

            List<char> missing;
            Require(reloaded.HasCharacters(TestText, out missing) && missing.Count == 0,
                "Saved TMP Font Asset is missing verification glyphs: " + new string(missing.ToArray()));

            Debug.Log(
                "SOURCE HAN SANS TMP FONT VERIFICATION PASSED\n" +
                "Source Font: " + sourceFont.name + "\n" +
                "TMP Asset: " + AssetPath + "\n" +
                "Atlas: 2048x2048, SDFAA, Dynamic\n" +
                "Multi Atlas Textures: enabled\n" +
                "Missing Glyphs: 0");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
