namespace DeskWarrior.Managers.Repositories
{
    /// <summary>
    /// 데이터 저장소 기본 인터페이스
    /// </summary>
    public interface IRepository<T> where T : class, new()
    {
        /// <summary>
        /// 데이터 로드
        /// </summary>
        T Load();

        /// <summary>
        /// 데이터 저장
        /// </summary>
        void Save(T data);

        /// <summary>
        /// 변경 여부 플래그
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// 변경 플래그 설정
        /// </summary>
        void MarkDirty();

        /// <summary>
        /// 변경 플래그 초기화
        /// </summary>
        void ClearDirty();
    }
}
