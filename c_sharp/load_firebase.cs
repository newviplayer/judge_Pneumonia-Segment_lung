using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Storage;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using OpenCvSharp;

namespace use_lung_model_project_v2
{
    public class load_firebase
    {
        private FirestoreDb db;
        private StorageClient storage;
        private FirebaseStorage fb_storage;
        public load_firebase()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"json_file";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
            db = FirestoreDb.Create("process-lung");
            fb_storage = new FirebaseStorage("process-lung.appspot.com");
            storage = StorageClient.Create();
        }

        public static FirebaseAuthConfig GetFbAC()
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = "APIKEY",
                AuthDomain = "process-lung.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                    new GoogleProvider().AddScopes("email"),
                    new EmailProvider()
                }
            };

            return config;
        }

        public FirestoreDb GetFirestoreDb()
        {
            return db;
        }

        public StorageClient GetStorageClient()
        {
            return storage;
        }

        public FirebaseStorage GetFbStorage()
        {
            return fb_storage;
        }

        public static async Task DeleteImage(string user_uid, string file_name)
        {
            // 이미지 삭제
            load_firebase firebaseLoader = new load_firebase();
            FirebaseStorage storage = firebaseLoader.GetFbStorage();

            string filepath = $"{user_uid}/{file_name}.jpg";

            await storage.Child(filepath).DeleteAsync();
        }
    }
}
