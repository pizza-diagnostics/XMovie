namespace XMovie.DataModel
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using SQLite.CodeFirst;

    public class XMovieContext : DbContext
    {
        // コンテキストは、アプリケーションの構成ファイル (App.config または Web.config) から 'XMovieContext' 
        // 接続文字列を使用するように構成されています。既定では、この接続文字列は LocalDb インスタンス上
        // の 'XMovie.DataModel.XMovieContext' データベースを対象としています。 
        // 
        // 別のデータベースとデータベース プロバイダーまたはそのいずれかを対象とする場合は、
        // アプリケーション構成ファイルで 'XMovieContext' 接続文字列を変更してください。
        public XMovieContext()
            : base("name=XMovieContext")
        {
        }

        public virtual DbSet<Movie> Movies { get; set; }
        public virtual DbSet<Thumbnail> Thumbnails { get; set; }
        public virtual DbSet<TagCategory> TagCategories { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<TagMap> TagMaps { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<XMovieContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }
    }

}