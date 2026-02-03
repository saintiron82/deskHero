using System.Collections.Generic;
using DeskWarrior.Models;

namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// UserSave.json 저장소
    /// RELEASE: 암호화된 저장 (SecureJsonFileRepository)
    /// DEBUG: 평문 JSON (개발 편의)
    /// </summary>
    public class UserSaveRepository : SecureJsonFileRepository<UserSave>
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

        /// <summary>
        /// 무결성 검증 실패 시 처리
        /// 데이터 초기화 (사용자 선택)
        /// </summary>
        protected override void OnIntegrityViolation()
        {
            base.OnIntegrityViolation();
            System.Diagnostics.Debug.WriteLine("[UserSaveRepo] Save data corrupted or tampered. Resetting to default.");
        }
    }
}
