namespace GameServerAdmin.Common.Exceptions.Notice
{
    public class NoticeNotFoundException : NotFoundException
    {
        public NoticeNotFoundException(long noticeId) : base(ErrorCode.NOTICE_NOT_FOUND, $"공지사항을 찾을 수 없습니다. noticeId = {noticeId}")
        {

        }
    }
}
