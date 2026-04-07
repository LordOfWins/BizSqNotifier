// Models/ProductCategory.cs (신규)
namespace BizSqNotifier.Models
{
    public enum ProductCategory
    {
        Office,      // ~인실
        Address,     // 개인사업자, 법인사업자
        Desk,        // 스마트데스크
        Excluded     // 모아즈 (발송 제외)
    }
}
