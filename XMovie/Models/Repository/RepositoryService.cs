using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using XMovie.Common;
using XMovie.Models.Data;

namespace XMovie.Models.Repository
{
    public class RepositoryService : IDisposable
    {
        private XMovieContext context;

        private MovieRepository movieRepository;
        private TagRepository tagRepository;
        private TagMapRepository tagMapRepository;
        private ThumbnailRepository thumbnailRepository;
        private TagCategoryRepository tagCategoryRepository;

        public RepositoryService()
        {
            context = new XMovieContext();

            movieRepository = new MovieRepository(context);
            thumbnailRepository = new ThumbnailRepository(context);
            tagCategoryRepository = new TagCategoryRepository(context);
            tagRepository = new TagRepository(context);
            tagMapRepository = new TagMapRepository(context);
        }

        #region Movie
        public int MovieCount()
        {
            return movieRepository.Count();
        }

        public IList<Movie> GetAllMovie()
        {
            return movieRepository.GetAll().ToList();
        }

        public Movie FindMovie(params object[] keys)
        {
            return movieRepository.Find(keys);
        }

        public IList<Movie> FindAllMovies(Expression<Func<Movie, bool>> predicate)
        {
            return movieRepository.FindAll(predicate).ToList();
        }

        public Movie FindMovieAtPath(string path)
        {
            return movieRepository.FindMovieAtPath(path);
        }

        public IList<Movie> FindDuplicateMovies(SortDescriptor sort)
        {
            return movieRepository.FindDuplicateMovies(sort).ToList();
        }

        public IList<Movie> FindMoviesByPathKeys(List<string> pathKeys, SortDescriptor sort)
        {
            return movieRepository.FindMoviesByPathKeys(pathKeys, sort).ToList();
        }

        public IList<Movie> FindMoviesByTags(List<string> tags, SortDescriptor sort)
        {
            if (tags.Count() > 0)
            {
                // 1. LIKE OR でTagを抽出
                var tagList = tagRepository.FindAllTagsOr(tags).Select(t => t.TagId);
                // 2. TagMapからMovieIdを抽出
                var movieIdList = tagMapRepository.FindAll(tm => tagList.Contains(tm.TagId))
                                                  .GroupBy(t => t.MovieId)
                                                  .Select(g => new { g.Key, Count = g.Count() })
                                                  .Where(g => g.Count >= tags.Count())
                                                  .Select(g => g.Key)
                                                  .ToList();

                // 3. MoviesからMovieを抽出
                return movieRepository.FindMovies(m => movieIdList.Contains(m.MovieId), sort).ToList();
            }
            else
            {
                return sort.MovieSort(movieRepository.GetAll()).ToList();
            }
        }

        public Movie ApplyMovie(string movieId, Action<Movie> action)
        {
            return movieRepository.Apply(movieId, action);
        }

        public void UpdateMovie(Movie movie)
        {
            movieRepository.Update(movie);
        }

        public bool IsExistMovieAtPath(string path)
        {
            return movieRepository.GetAll().ToList().Any(m => Util.IsEqualsNormalizedPath(m.Path, path));
        }

        public void InsertMovie(Movie movie)
        {
            movieRepository.Insert(movie);
        }

        public void RemoveMovie(Movie movie)
        {
            // 1. サムネイル削除
            RemoveMovieThumbnails(movie.MovieId);
            // 2. タグ削除
            RemoveMovieTagMaps(movie.MovieId);
            // 3. 動画削除
            movieRepository.Delete(movie);
        }

        #endregion

        #region Category
        public IList<TagCategory> GetAllCategories()
        {
            return tagCategoryRepository.GetAll().ToList();
        }

        public bool IsExistCategory(string name)
        {
            return tagCategoryRepository.IsExistTagCategory(name);
        }

        public TagCategory InsertNewCategory(string name)
        {
            return tagCategoryRepository.InsertNewCategory(name);
        }

        public void RemoveTagCategory(int categoryId)
        {
            // 1. 削除するタグ
            var deleteTags = tagRepository.FindAll(t => t.TagCategoryId == categoryId);
            var deleteTagIds = deleteTags.Select(t => t.TagId);

            // 2. TagMapsから削除
            var deleteTagMaps = tagMapRepository.FindAll(tm => deleteTagIds.Contains(tm.TagId));
            tagMapRepository.DeleteRange(deleteTagMaps.ToList());

            // 3. Tagsから削除
            tagRepository.DeleteRange(deleteTags.ToList());

            // 4. Categoryを削除
            tagCategoryRepository.Delete(tagCategoryRepository.Find(categoryId));
        }
        #endregion

        #region Tag
        public IList<Tag> FindCategoryTags(int categoryId)
        {
            return tagRepository.FindAll(t => t.TagCategoryId == categoryId).ToList();
        }

        public Tag InsertNewTag(string tagName, int tagCategoryId)
        {
            return tagRepository.InsertNewTag(tagName, tagCategoryId);
        }

        public IList<Tag> FindMovieTags(IList<string> movieIdList)
        {
            var tagIdList = tagMapRepository.FindAll(tm => movieIdList.Contains(tm.MovieId)).Select(tm => tm.TagId);
            return tagRepository.FindAll(t => tagIdList.Contains(t.TagId)).Distinct().ToList();
        }

        public IList<Tag> FindMovieTags(IList<string> movieIdList, int categoryId)
        {
            var tagIdList = tagMapRepository.FindAll(tm => movieIdList.Contains(tm.MovieId)).Select(tm => tm.TagId);
            return tagRepository.FindAll(t => tagIdList.Contains(t.TagId) && t.TagCategoryId == categoryId).Distinct().ToList();
        }

        public IList<Tag> FindUnusedTags()
        {
            var usedTagId = tagMapRepository.GetAll().GroupBy(tm => tm.TagId).Select(g => g.Key);
            return tagRepository.FindAll(t => !usedTagId.Contains(t.TagId)).ToList();
        }

        public void RemoveTags(IList<Tag> tags)
        {
            tagRepository.DeleteRange(tags);
        }
        #endregion

        #region TagMap
        public void RemoveTagMaps(int tagId, IList<string> movieIdList)
        {
            var tagMaps = tagMapRepository.FindAll(tm => tm.TagId == tagId && movieIdList.Contains(tm.MovieId));
            foreach (var tm in tagMaps)
            {
                tagMapRepository.Delete(tm);
            }
        }

        public void RemoveMovieTagMaps(string movieId)
        {
            var tagMaps = tagMapRepository.FindAll(tm => tm.MovieId.Equals(movieId));
            tagMapRepository.DeleteRange(tagMaps.ToList());
        }
        #endregion

        #region Thumbnail
        public IList<Thumbnail> FindMovieThumbnails(string movieId)
        {
            return thumbnailRepository.FindMovieThumbnails(movieId).ToList();
        }

        public IList<Thumbnail> FindAllThumbnails(Expression<Func<Thumbnail, bool>> predicate)
        {
            return thumbnailRepository.FindAll(predicate).ToList();
        }

        public void RemoveMovieThumbnails(string movieId)
        {
            thumbnailRepository.DeleteRange(FindMovieThumbnails(movieId));
        }
        #endregion


        #region complex
        public void SetTagToMovies(Tag tag, IList<string> movieIdList)
        {
            // 動画へのタグ追加が重複しないよう、すでに同タグが付いている動画を抽出
            var ignores = tagMapRepository.FindAll(tm => tm.TagId == tag.TagId && movieIdList.Contains(tm.MovieId))
                                          .Select(tm => tm.MovieId)
                                          .ToList();

            foreach (var id in movieIdList)
            {
                if (!ignores.Contains(id))
                {
                    var tagMap = new TagMap()
                    {
                        TagId = tag.TagId,
                        MovieId = id
                    };
                    tagMapRepository.Insert(tagMap);
                }
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。
                if (context != null)
                {
                    context.Dispose();
                    context = null;
                }

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        ~RepositoryService() {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
