using System;
using System.Collections.Generic;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace SRQD.Editor
{
    /// <summary>
    /// Lets the manifest know that we will try to launch Synth Riders, so it's supported on newer versions.
    /// Also lets it set the necessary permissions to modify files
    /// </summary>
    internal class ModifyAndroidManifestSRQD : OpenXRFeatureBuildHooks
    {
        public override int callbackOrder => 2;

        public override Type featureType => typeof(MetaQuestFeature);

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        protected override ManifestRequirement ProvideManifestRequirementExt()
        {
            var elementsToRemove = new List<ManifestElement>();
            var elementsToAdd = new List<ManifestElement>
            {
                // Let the system know we'll try to launch a different app
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "queries", "package" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.kluge.SynthRiders" }
                    }
                },
                // Get permissions to change files on the system
                new ManifestElement()
                {
                    ElementPath = new List<string> { "manifest", "uses-permission" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "android.permission.MANAGE_EXTERNAL_STORAGE" }
                    }
                }
            };

            Debug.Log("SRQD adding to manifest");
            return new ManifestRequirement
            {
                SupportedXRLoaders = new HashSet<Type>()
                {
                    typeof(OpenXRLoader)
                },
                NewElements = elementsToAdd,
                RemoveElements = elementsToRemove
            };
        }
    }
}
