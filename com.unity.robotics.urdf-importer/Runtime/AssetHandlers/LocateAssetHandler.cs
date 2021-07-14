﻿/*
© Siemens AG, 2018
Author: Suzannah Smith (suzannah.smith@siemens.com)
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.IO;
using System;
using UnityEngine;

namespace Unity.Robotics.UrdfImporter
{
    public static class LocateAssetHandler
    {
        public static T FindUrdfAsset<T>(string urdfFileName) where T : UnityEngine.Object
        {
            string fileAssetPath = UrdfAssetPathHandler.GetRelativeAssetPathFromUrdfPath(urdfFileName);

            // check if it is an asset tha requires post processing (AIRO-908)
            var originalUrdfPath = UrdfAssetPathHandler.GetRelativeAssetPathFromUrdfPath(urdfFileName, false);
            if (originalUrdfPath.ToLower().EndsWith(".stl"))
            {// it is an asset that requires post processing
                if (UrdfRobotExtensions.importsettings.OverwriteExistingPrefabs || !RuntimeUrdf.AssetExists(fileAssetPath, true))
                {// post process again to (re)create prefabs
                    StlAssetPostProcessor.PostprocessStlFile(originalUrdfPath);
                }                
            }

            T assetObject = RuntimeUrdf.AssetDatabase_LoadAssetAtPath<T>(fileAssetPath);

            if (assetObject)
                return assetObject;

            //If asset was not found, let user choose whether to search for
            //or ignore the missing asset.
            string invalidPath = fileAssetPath ?? urdfFileName;
            int option = RuntimeUrdf.EditorUtility_DisplayDialogComplex("Urdf Importer: Asset Not Found",
                "Current root folder: " + UrdfAssetPathHandler.GetPackageRoot() +
                "\n\nExpected asset path: " + invalidPath,
                "Locate Asset",
                "Ignore Missing Asset",
                "Locate Root Folder");

            switch (option)
            {
                case 0:
                    fileAssetPath = LocateAssetFile(invalidPath);
                    break;
                case 1: break;
                case 2:
                    fileAssetPath = LocateRootAssetFolder<T>(urdfFileName);
                    break;
            }

            assetObject = (T) RuntimeUrdf.AssetDatabase_LoadAssetAtPath(fileAssetPath, typeof(T));
            if (assetObject != null)
                return assetObject;

            ChooseFailureOption(urdfFileName);
            return null;
        }

        private static string LocateRootAssetFolder<T>(string urdfFileName) where T : UnityEngine.Object
        {
            string newAssetPath = RuntimeUrdf.EditorUtility_OpenFolderPanel(
                "Locate package root folder",
                Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets"),
                "");

            if (UrdfAssetPathHandler.IsValidAssetPath(newAssetPath))
            {
                UrdfAssetPathHandler.SetPackageRoot(newAssetPath, true);
            }
            else
            {
                Debug.LogWarning("Selected package root " + newAssetPath + " is not within the Assets folder.");
            }
            return UrdfAssetPathHandler.GetRelativeAssetPathFromUrdfPath(urdfFileName);
        }

        private static string LocateAssetFile(string invalidPath)
        {
            string fileExtension = Path.GetExtension(invalidPath)?.Replace(".", "");

            string newPath = RuntimeUrdf.EditorUtility_OpenFilePanel(
                "Couldn't find asset at " + invalidPath + ". Select correct file.",
                UrdfAssetPathHandler.GetPackageRoot(),
                fileExtension);

            return UrdfAssetPathHandler.GetRelativeAssetPath(newPath);
        }

        private static void ChooseFailureOption(string urdfFilePath)
        {
            if (!RuntimeUrdf.EditorUtility_DisplayDialog(
                "Urdf Importer: Missing Asset",
                "Missing asset " + Path.GetFileName(urdfFilePath) +
                " was ignored or could not be found.\n\nContinue URDF Import?",
                "Yes",
                "No"))
            {
                throw new InterruptedUrdfImportException("User cancelled URDF import. Model may be incomplete.");
            }
        }
        
        private class InterruptedUrdfImportException : Exception
        {
            public InterruptedUrdfImportException(string message) : base(message)
            {
            }
        }
    }
}
