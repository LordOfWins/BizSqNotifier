// Helpers/ProductClassifier.cs (신규)
namespace BizSqNotifier.Helpers
{
    public static class ProductClassifier
    {
        public static ProductCategory Classify(string prdPrd)
        {
            if (string.IsNullOrWhiteSpace(prdPrd))
                return ProductCategory.Excluded;

            string trimmed = prdPrd.Trim();

            // 오피스: ~인실
            if (trimmed.EndsWith("인실"))
                return ProductCategory.Office;

            // 주소지: 개인사업자, 법인사업자
            if (trimmed == "개인사업자" || trimmed == "법인사업자")
                return ProductCategory.Address;

            // 스마트데스크
            if (trimmed == "스마트데스크")
                return ProductCategory.Desk;

            // 그 외 (모아즈 포함) → 제외
            return ProductCategory.Excluded;
        }

        public static bool IsSendTarget(string prdPrd)
        {
            return Classify(prdPrd) != ProductCategory.Excluded;
        }
    }
}
