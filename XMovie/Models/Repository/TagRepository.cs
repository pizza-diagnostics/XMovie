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
    }
}
