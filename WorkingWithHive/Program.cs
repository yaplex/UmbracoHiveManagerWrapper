namespace WorkingWithHive
{
    using System;
    using System.IO;
    using System.Linq;
    using Umbraco.Cms.Web;
    using Umbraco.Cms.Web.FluentExtensions;
    using Umbraco.Cms.Web.Model;
    using Umbraco.Cms.Web.Model.BackOffice.Editors;
    using Umbraco.Framework;
    using Umbraco.Framework.Persistence.Model;
    using Umbraco.Framework.Persistence.Model.Attribution;
    using Umbraco.Framework.Persistence.Model.Constants.Schemas;
    using Umbraco.Hive;
    using Umbraco.Hive.ProviderGrouping;
    using Umbraco.Hive.RepositoryTypes;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var wrapper = new HiveManagerWrapper();
            IHiveManager hive = wrapper.GetHiveManager();
            var all = hive.Query<Content, IContentStore>().ToList();
            foreach (Content content in all)
            {
                Console.WriteLine(content.Id);
            }

            Content parent =
                hive.Query<Content, IContentStore>().SingleOrDefault(
                    x => x.Id == new HiveId("content://p__nhibernate/v__guid/cebc1f3a6d2240698c81a02d014cd7f4"));
            string releaseBody = File.ReadAllText(@"d:\temp\testrelease.html");
            string releaseContact = File.ReadAllText(@"D:\temp\testcontact.html");
            for (int i = 0; i < 400; i++)
            {
                IContentRevisionBuilderStep<TypedEntity, IContentStore> child = hive.Cms<IContentStore>()
                    .NewRevision("Child Page" + Guid.NewGuid(), "my-child-page-" + i, "pressRelease", false)
                    .SetValue("headline", "Test release: " + Guid.NewGuid())
                    .SetValue("releaseDate", DateTime.Now.AddHours(-i).AddMinutes(-i))
                    .SetValue("contactInformation", releaseContact)
                    .SetValue("releaseCompany", "Test company: " + Guid.NewGuid())
                    .SetValue("releaseContent", releaseBody)
                    .SetValue("location", "Toronto, ON")
                    .SetParent(parent.Id)
                    .Publish();
                var p2 = child.Commit();
                Console.WriteLine(p2.Content.Id);                
            }

        }
    }
} 



/*           IEnumerable<TypedEntity> allDocuments =
                hive.GetReader<IContentStore>().CreateReadonly().Repositories.GetAll<TypedEntity>();
            foreach (TypedEntity entity in allDocuments)
            {
                foreach (TypedAttribute attribute in entity.Attributes)
                {
                    foreach (var val in attribute.Values)
                    {
                        Console.WriteLine("{0}: {1}", attribute.AttributeDefinition.Name, val.Value);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");

            Content mypage =
                hive.Query<Content, IContentStore>().SingleOrDefault(
                    x => x.Id == new HiveId("content://p__nhibernate/v__guid/a315916aa49d41358232a02b00cee46a"));
            foreach (TypedAttribute attribute in mypage.Attributes)
            {
                Console.WriteLine("{0}: {1}", attribute.AttributeDefinition.Name, attribute.Values.FirstOrDefault());
            }*/