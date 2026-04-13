using System;
using System.IO;
using BizSqNotifier.Services;
using Newtonsoft.Json;

namespace BizSqNotifier.Config
{
    /// <summary>
    /// settings.json 기반 사용자 설정 모델 + 로드/저장.
    /// 관리자 설정 화면에서 편집한 값을 JSON 파일로 영속화합니다.
    ///
    /// 파일 위치: 실행 파일과 같은 폴더의 settings.json
    ///           (AppSettings.SettingsFilePath로 지정)
    /// </summary>
    public sealed class UserSettings
    {
        #region 싱글톤

        private static UserSettings _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 현재 설정 인스턴스. 최초 호출 시 파일에서 로드합니다.
        /// </summary>
        public static UserSettings Current
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = Load();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 설정을 다시 로드합니다 (파일 변경 후 반영 시).
        /// </summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _instance = Load();
            }
        }

        #endregion

        #region 복합기 로그인 정보

        /// <summary>복합기 ID (입주 안내 메일에 치환)</summary>
        [JsonProperty("printerLoginId")]
        public string PrinterLoginId { get; set; } = string.Empty;

        /// <summary>복합기 PW (입주 안내 메일에 치환)</summary>
        [JsonProperty("printerLoginPw")]
        public string PrinterLoginPw { get; set; } = string.Empty;

        #endregion

        #region 발송 시각 설정

        /// <summary>자동 발송 활성화 여부 (기본 false — 수동 확인 후 활성화)</summary>
        [JsonProperty("autoSendEnabled")]
        public bool AutoSendEnabled { get; set; } = false;

        /// <summary>입주/퇴실/갱신자동 발송 시각 (HH:mm, 기본 09:00)</summary>
        [JsonProperty("generalSendTime")]
        public string GeneralSendTime { get; set; } = "09:00";

        /// <summary>미납 안내 발송 시각 (HH:mm, 기본 13:00 — 고객 지정)</summary>
        [JsonProperty("unpaidSendTime")]
        public string UnpaidSendTime { get; set; } = "13:00";

        #endregion

        #region 발송 기준일 설정

        /// <summary>미납 1차 기준일 (납부일 경과 N일, 기본 3)</summary>
        [JsonProperty("unpaid1stDays")]
        public int Unpaid1stDays { get; set; } = 3;

        /// <summary>미납 2차 기준일 (납부일 경과 N일, 기본 10)</summary>
        [JsonProperty("unpaid2ndDays")]
        public int Unpaid2ndDays { get; set; } = 10;

        /// <summary>미납 최종 기준일 (납부일 경과 N일, 기본 15)</summary>
        [JsonProperty("unpaidFinalDays")]
        public int UnpaidFinalDays { get; set; } = 15;

        /// <summary>갱신자동(adBox/회원제) 기준일 (계약종료 N일 전, 기본 8)</summary>
        [JsonProperty("renewalAutoDays")]
        public int RenewalAutoDays { get; set; } = 8;

        /// <summary>갱신수동(오피스) 목록 표시 기준일 (계약종료 N일 전, 기본 33)</summary>
        [JsonProperty("renewalManualDays")]
        public int RenewalManualDays { get; set; } = 33;

        /// <summary>퇴실 안내 기준일 (퇴실예정 N일 전, 기본 1)</summary>
        [JsonProperty("moveOutDays")]
        public int MoveOutDays { get; set; } = 1;

        #endregion

        #region 발송 시각 파싱 헬퍼

        /// <summary>GeneralSendTime을 TimeSpan으로 변환합니다.</summary>
        [JsonIgnore]
        public TimeSpan GeneralSendTimeSpan => ParseTime(GeneralSendTime, new TimeSpan(9, 0, 0));

        /// <summary>UnpaidSendTime을 TimeSpan으로 변환합니다.</summary>
        [JsonIgnore]
        public TimeSpan UnpaidSendTimeSpan => ParseTime(UnpaidSendTime, new TimeSpan(13, 0, 0));

        private static TimeSpan ParseTime(string timeStr, TimeSpan defaultValue)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
                return defaultValue;

            if (TimeSpan.TryParse(timeStr, out var result))
                return result;

            return defaultValue;
        }

        #endregion

        #region 로드 / 저장

        /// <summary>
        /// settings.json에서 설정을 로드합니다.
        /// 파일이 없거나 파싱 실패 시 기본값을 반환합니다.
        /// </summary>
        private static UserSettings Load()
        {
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                AppLog.Info("settings.json 없음 — 기본값 사용");
                var defaults = new UserSettings();
                defaults.Save(); // 기본값으로 파일 생성
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var settings = JsonConvert.DeserializeObject<UserSettings>(json);

                if (settings == null)
                {
                    AppLog.Warn("settings.json 역직렬화 결과 null — 기본값 사용");
                    return new UserSettings();
                }

                AppLog.Info("settings.json 로드 완료");
                return settings;
            }
            catch (Exception ex)
            {
                AppLog.Error("settings.json 로드 실패 — 기본값 사용", ex);
                return new UserSettings();
            }
        }

        /// <summary>
        /// 현재 설정을 settings.json에 저장합니다.
        /// </summary>
        public void Save()
        {
            var path = GetFilePath();

            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json, System.Text.Encoding.UTF8);
                AppLog.Info("settings.json 저장 완료");
            }
            catch (Exception ex)
            {
                AppLog.Error("settings.json 저장 실패", ex);
                throw;
            }
        }

        /// <summary>
        /// settings.json 파일 절대 경로를 반환합니다.
        /// </summary>
        private static string GetFilePath()
        {
            var relativePath = AppSettings.SettingsFilePath;

            if (Path.IsPathRooted(relativePath))
                return relativePath;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }

        #endregion
    }
}
