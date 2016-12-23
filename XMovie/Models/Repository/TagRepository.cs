using System;
using System.Collections.Generic;
using System.Linq;
using XMovie.Models.Data;

namespace XMovie.Models.Repository
{
    public class TagRepository : Repository<Tag>
    {
        public TagRepository(System.Data.Entity.DbContext context) : base(context) { }

        /// <summary>
        /// 新規タグを追加して返却する。
        /// タグがある場合(同名/同カテゴリ)がある場合は追加せずに既存タグを返却する。
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="tagCategoryId"></param>
        /// <returns></returns>
        public Tag InsertNewTag(string tagName, int tagCategoryId)
        {
            Tag tag = dbSet.Where(t => t.TagCategoryId == tagCategoryId && t.Name.Equals(tagName)).FirstOrDefault();
            if (tag == null)
            {
                tag = new Tag()
                {
                    TagCategoryId = tagCategoryId,
                    Name = tagName,
                };
                Insert(tag);
            }
            return tag;
        }

        public IQueryable<Tag> FindAllTagsOr(IList<string> tagKeys)
        {
            // CHARINDEXになるためSQLを記述する
            var q = "select * from tags where " + String.Join(" or ", tagKeys.Select((k, i) => $"Name like @p{i}"));

            return context.Database.SqlQuery<Tag>(q, tagKeys.Select(k => $"%{k}%").ToArray()).AsQueryable<Tag>();
        }
    }
}
