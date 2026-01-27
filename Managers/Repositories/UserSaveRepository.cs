using System.Collections.Generic;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// UserSave.json 저장소
    /// </summary>
    public class UserSaveRepository : JsonFileRepository<UserSave>
    {
        public UserSaveRepository(string filePath) : base(filePath)
        {
        }

        /// <summary>
        /// 로드 후 마이그레이션 처리
        /// </summary>
        protected override void OnLoaded(UserSave data)
        {
            // Null 체크 및 초기화 (하위 호환성)
            data.PermanentCurrency ??= new PermanentCurrency();
            data.PermanentStats ??= new PermanentStats();
            data.PermanentUpgrades ??= new List<PermanentUpgradeProgress>();
        }
    }
}
