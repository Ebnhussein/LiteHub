using System;
using System.Collections.Generic;
using System.Globalization;

// Interface languages. Every UI string is written in English in the code and looked up here;
// a missing entry just shows the English text, so a language can never break the app.
static class Loc
{
    public static readonly string[] Codes = { "en", "ar", "es", "fr", "de", "pt", "tr", "ru" };
    public static readonly string[] Names = { "English", "العربية", "Español", "Français", "Deutsch", "Português", "Türkçe", "Русский" };
    public static string Lang = "en";

    static Dictionary<string, string> map = new Dictionary<string, string>();

    public static bool IsRtl { get { return Lang == "ar"; } }

    // "" = follow Windows (when supported), otherwise a code from Codes.
    public static void Set(string code)
    {
        if (string.IsNullOrEmpty(code)) code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        int i = Array.IndexOf(Codes, code);
        Lang = i < 0 ? "en" : code;
        map = new Dictionary<string, string>();
        if (i <= 0) return;
        foreach (string row in Rows)
        {
            string[] p = row.Split('|');
            if (p.Length == Codes.Length && p[i].Length > 0) map[p[0]] = p[i];
        }
    }

    public static string T(string s)
    {
        string r;
        return s != null && map.TryGetValue(s, out r) ? r : s;
    }

    public static string F(string s, params object[] a) { return string.Format(T(s), a); }


    // Every row needs all 8 columns and the same {0} placeholders as English.
    public static bool Ok()
    {
        foreach (string row in Rows)
        {
            string[] p = row.Split('|');
            if (p.Length != Codes.Length) return false;
            for (int i = 1; i < p.Length; i++) if (p[i].Contains("{0}") != p[0].Contains("{0}")) return false;
        }
        return true;
    }
    // en|ar|es|fr|de|pt|tr|ru
    static readonly string[] Rows = {
        "Search accounts and actions|ابحث في الحسابات والإجراءات|Buscar cuentas y acciones|Rechercher comptes et actions|Konten und Aktionen suchen|Buscar contas e ações|Hesap ve eylemlerde ara|Поиск аккаунтов и действий",
        "Search services|ابحث عن خدمة|Buscar servicios|Rechercher un service|Dienst suchen|Buscar serviços|Hizmet ara|Поиск сервисов",
        "Performance|الأداء|Rendimiento|Performances|Leistung|Desempenho|Performans|Производительность",
        "Memory used by LiteHub|الذاكرة التي يستخدمها LiteHub|Memoria que usa LiteHub|Mémoire utilisée par LiteHub|Von LiteHub belegter Speicher|Memória usada pelo LiteHub|LiteHub'ın kullandığı bellek|Память, занятая LiteHub",
        "Per-account numbers are estimates; shared browser engine: {0} MB|أرقام الحسابات تقديرية؛ محرك المتصفح المشترك: {0} ميجا|Las cifras por cuenta son estimadas; motor del navegador compartido: {0} MB|Chiffres par compte estimés ; moteur de navigateur partagé : {0} Mo|Werte pro Konto sind Schätzungen; gemeinsame Browser-Engine: {0} MB|Valores por conta são estimativas; mecanismo de navegador compartilhado: {0} MB|Hesap başına değerler tahminidir; paylaşılan tarayıcı motoru: {0} MB|Значения по аккаунтам приблизительные; общий движок браузера: {0} МБ",
        "Sleep inactive|إسبات غير النشطة|Dormir inactivas|Mettre en veille les inactifs|Inaktive schlafen legen|Suspender inativas|Boştakileri uyut|Усыпить неактивные",
        "Sleep|إسبات|Dormir|Veille|Schlafen|Suspender|Uyut|Усыпить",
        "Active|نشط|Activa|Actif|Aktiv|Ativa|Etkin|Активен",
        "Running|يعمل|En ejecución|En cours|Läuft|Em execução|Çalışıyor|Работает",
        "Suspended|موقوف مؤقتاً|Suspendida|Suspendu|Angehalten|Suspensa|Askıda|Приостановлен",
        "Not loaded|غير محمّل|No cargada|Non chargé|Nicht geladen|Não carregada|Yüklenmedi|Не загружен",
        "Add another account|إضافة حساب آخر|Añadir otra cuenta|Ajouter un autre compte|Weiteres Konto hinzufügen|Adicionar outra conta|Başka hesap ekle|Добавить ещё аккаунт",
        "Same service, its own login and name|نفس الخدمة بتسجيل دخول واسم منفصلين|Mismo servicio, con su propia sesión y nombre|Même service, avec sa propre connexion et son nom|Gleicher Dienst, eigene Anmeldung und eigener Name|Mesmo serviço, com login e nome próprios|Aynı hizmet, kendi oturumu ve adıyla|Тот же сервис, свой вход и имя",
        "Add|إضافة|Añadir|Ajouter|Hinzufügen|Adicionar|Ekle|Добавить",
        "Workspace|مساحة العمل|Espacio de trabajo|Espace de travail|Arbeitsbereich|Espaço de trabalho|Çalışma alanı|Рабочее пространство",
        "All|الكل|Todo|Tout|Alle|Tudo|Tümü|Все",
        "No workspace|بدون مساحة|Sin espacio|Aucun espace|Kein Arbeitsbereich|Sem espaço|Alan yok|Без пространства",
        "New workspace...|مساحة عمل جديدة...|Nuevo espacio...|Nouvel espace...|Neuer Arbeitsbereich...|Novo espaço...|Yeni alan...|Новое пространство...",
        "Workspace name (for example Work)|اسم مساحة العمل (مثل الشغل)|Nombre del espacio (p. ej. Trabajo)|Nom de l'espace (ex. Travail)|Name des Arbeitsbereichs (z. B. Arbeit)|Nome do espaço (ex. Trabalho)|Alan adı (örn. İş)|Название пространства (например, Работа)",
        "Workspace: {0}|مساحة العمل: {0}|Espacio: {0}|Espace : {0}|Arbeitsbereich: {0}|Espaço: {0}|Alan: {0}|Пространство: {0}",
        "Inbox|الوارد|Bandeja de entrada|Boîte de réception|Posteingang|Caixa de entrada|Gelen kutusu|Входящие",
        "Mark all read|تعليم الكل كمقروء|Marcar todo como leído|Tout marquer comme lu|Alle als gelesen markieren|Marcar tudo como lido|Tümünü okundu yap|Прочитать все",
        "Clear|مسح|Vaciar|Effacer|Leeren|Limpar|Temizle|Очистить",
        "No notifications yet. New messages from all your accounts will appear here.|لا توجد إشعارات بعد. الرسائل الجديدة من كل حساباتك ستظهر هنا.|Aún no hay notificaciones. Los mensajes nuevos de todas tus cuentas aparecerán aquí.|Pas encore de notifications. Les nouveaux messages de tous vos comptes apparaîtront ici.|Noch keine Benachrichtigungen. Neue Nachrichten aller Konten erscheinen hier.|Nenhuma notificação ainda. As novas mensagens de todas as suas contas aparecerão aqui.|Henüz bildirim yok. Tüm hesaplarınızdan gelen yeni mesajlar burada görünecek.|Уведомлений пока нет. Новые сообщения из всех аккаунтов появятся здесь.",
        "Enter = open     Esc = close|Enter = فتح     Esc = إغلاق|Enter = abrir     Esc = cerrar|Entrée = ouvrir     Échap = fermer|Enter = öffnen     Esc = schließen|Enter = abrir     Esc = fechar|Enter = aç     Esc = kapat|Enter = открыть     Esc = закрыть",
        "Search|بحث|Buscar|Rechercher|Suchen|Buscar|Ara|Поиск",
        "Remember inbox|تذكّر الوارد|Recordar la bandeja|Mémoriser la boîte|Posteingang merken|Lembrar caixa de entrada|Gelen kutusunu hatırla|Помнить входящие",
        "Keep a local copy of notification previews on this PC|احتفظ بنسخة محلية من معاينات الإشعارات على هذا الجهاز|Guarda una copia local de las vistas previas de notificaciones en este PC|Garder une copie locale des aperçus de notifications sur ce PC|Lokale Kopie der Benachrichtigungsvorschauen auf diesem PC behalten|Manter uma cópia local das prévias de notificações neste PC|Bildirim önizlemelerinin yerel bir kopyasını bu bilgisayarda tut|Хранить локальную копию превью уведомлений на этом ПК",
        "Do not disturb|عدم الإزعاج|No molestar|Ne pas déranger|Nicht stören|Não perturbe|Rahatsız etmeyin|Не беспокоить",
        "Mute pop-up notifications for a while (they still reach the Inbox)|اكتم الإشعارات المنبثقة لفترة (وتصل للوارد مع ذلك)|Silencia las notificaciones emergentes un tiempo (siguen llegando a la Bandeja)|Couper les notifications un moment (elles arrivent quand même dans la boîte)|Pop-up-Benachrichtigungen eine Weile stummschalten (sie landen weiter im Posteingang)|Silencie notificações por um tempo (elas ainda chegam à Caixa de entrada)|Açılır bildirimleri bir süre sessize al (yine de Gelen kutusuna düşer)|Отключить всплывающие уведомления на время (они всё равно попадут во «Входящие»)",
        "Off|إيقاف|Desactivado|Désactivé|Aus|Desligado|Kapalı|Выкл.",
        "30 minutes|30 دقيقة|30 minutos|30 minutes|30 Minuten|30 minutos|30 dakika|30 минут",
        "1 hour|ساعة واحدة|1 hora|1 heure|1 Stunde|1 hora|1 saat|1 час",
        "2 hours|ساعتان|2 horas|2 heures|2 Stunden|2 horas|2 saat|2 часа",
        "Until tomorrow 8:00|حتى 8:00 صباح الغد|Hasta mañana a las 8:00|Jusqu'à demain 8h00|Bis morgen 8:00|Até amanhã às 8:00|Yarın 08:00'e kadar|До завтра 8:00",
        "Until {0}|حتى {0}|Hasta {0}|Jusqu'à {0}|Bis {0}|Até {0}|{0} saatine kadar|До {0}",
        "Do not disturb: {0}|عدم الإزعاج: {0}|No molestar: {0}|Ne pas déranger : {0}|Nicht stören: {0}|Não perturbe: {0}|Rahatsız etmeyin: {0}|Не беспокоить: {0}",
        "New message|رسالة جديدة|Mensaje nuevo|Nouveau message|Neue Nachricht|Nova mensagem|Yeni mesaj|Новое сообщение",
        "Lock with passcode|قفل برمز المرور|Bloquear con código|Verrouiller par code|Mit Code sperren|Bloquear com código|Parolayla kilitle|Защитить кодом",
        "Ask for the passcode before opening this account (same passcode as Privacy)|اطلب رمز المرور قبل فتح هذا الحساب (نفس رمز الخصوصية)|Pedir el código antes de abrir esta cuenta (el mismo de Privacidad)|Demander le code avant d'ouvrir ce compte (le même que Confidentialité)|Code vor dem Öffnen dieses Kontos abfragen (derselbe wie bei Datenschutz)|Pedir o código antes de abrir esta conta (o mesmo de Privacidade)|Bu hesabı açmadan önce parolayı iste (Gizlilik ile aynı parola)|Запрашивать код перед открытием аккаунта (тот же, что в «Конфиденциальности»)",
        "Memory limit|حد الذاكرة|Límite de memoria|Limite de mémoire|Speicherlimit|Limite de memória|Bellek sınırı|Лимит памяти",
        "Sleep the least recently used accounts when LiteHub uses more than this|أوقف مؤقتاً الحسابات الأقل استخداماً عندما يتجاوز LiteHub هذا الحد|Suspende las cuentas menos usadas cuando LiteHub supere este límite|Mettre en veille les comptes les moins utilisés au-delà de cette limite|Am längsten ungenutzte Konten schlafen legen, wenn LiteHub mehr belegt|Suspender as contas menos usadas quando o LiteHub passar disso|LiteHub bunu aşınca en az kullanılan hesapları uyut|Усыплять давно не используемые аккаунты, когда LiteHub занимает больше",
        "No limit|بلا حد|Sin límite|Sans limite|Kein Limit|Sem limite|Sınırsız|Без лимита",
        "Shortcuts|الاختصارات|Atajos|Raccourcis|Tastenkürzel|Atalhos|Kısayollar|Горячие клавиши",
        "Jump to account 1-9|انتقل للحساب 1-9|Ir a la cuenta 1-9|Aller au compte 1-9|Zu Konto 1-9 springen|Ir para a conta 1-9|1-9. hesaba git|Перейти к аккаунту 1-9",
        "Next / previous account|الحساب التالي / السابق|Cuenta siguiente / anterior|Compte suivant / précédent|Nächstes / vorheriges Konto|Conta seguinte / anterior|Sonraki / önceki hesap|Следующий / предыдущий аккаунт",
        "Open or hide LiteHub|افتح LiteHub أو أخفِه|Abrir u ocultar LiteHub|Ouvrir ou masquer LiteHub|LiteHub öffnen oder ausblenden|Abrir ou ocultar o LiteHub|LiteHub'ı aç veya gizle|Открыть или скрыть LiteHub",
        "Close the search or a pop-up|أغلق البحث أو النافذة المنبثقة|Cerrar la búsqueda o una ventana|Fermer la recherche ou une fenêtre|Suche oder Fenster schließen|Fechar a busca ou uma janela|Aramayı veya pencereyi kapat|Закрыть поиск или окно",
        "Check for updates|التحقق من التحديثات|Buscar actualizaciones|Rechercher des mises à jour|Nach Updates suchen|Verificar atualizações|Güncellemeleri denetle|Проверить обновления",
        "Check|تحقق|Comprobar|Vérifier|Prüfen|Verificar|Denetle|Проверить",
        "You have the latest version ({0}).|لديك أحدث إصدار ({0}).|Tienes la última versión ({0}).|Vous avez la dernière version ({0}).|Du hast die neueste Version ({0}).|Você tem a versão mais recente ({0}).|En son sürüme sahipsiniz ({0}).|У вас последняя версия ({0}).",
        "Version {0} is available. Open the download page?|الإصدار {0} متاح. هل تفتح صفحة التنزيل؟|La versión {0} está disponible. ¿Abrir la página de descarga?|La version {0} est disponible. Ouvrir la page de téléchargement ?|Version {0} ist verfügbar. Download-Seite öffnen?|A versão {0} está disponível. Abrir a página de download?|{0} sürümü mevcut. İndirme sayfası açılsın mı?|Доступна версия {0}. Открыть страницу загрузки?",
        "No update address is set. Add a line UpdateUrl=... to settings.txt.|لم يُحدَّد عنوان للتحديثات. أضف سطر UpdateUrl=... إلى settings.txt.|No hay dirección de actualizaciones. Añade una línea UpdateUrl=... a settings.txt.|Aucune adresse de mise à jour. Ajoutez une ligne UpdateUrl=... à settings.txt.|Keine Update-Adresse festgelegt. Füge eine Zeile UpdateUrl=... in settings.txt hinzu.|Nenhum endereço de atualização definido. Adicione uma linha UpdateUrl=... ao settings.txt.|Güncelleme adresi ayarlanmadı. settings.txt'ye UpdateUrl=... satırı ekleyin.|Адрес обновлений не задан. Добавьте строку UpdateUrl=... в settings.txt.",
        "Could not check for updates.|تعذّر التحقق من التحديثات.|No se pudo buscar actualizaciones.|Impossible de rechercher les mises à jour.|Update-Suche fehlgeschlagen.|Não foi possível verificar atualizações.|Güncellemeler denetlenemedi.|Не удалось проверить обновления.",
        "Forgot passcode?|نسيت رمز المرور؟|¿Olvidaste el código?|Code oublié ?|Code vergessen?|Esqueceu o código?|Parolayı unuttunuz mu?|Забыли код?",
        "This removes the passcode and signs you out of every account. Your account list stays. Continue?|سيؤدي هذا إلى إزالة رمز المرور وتسجيل خروجك من كل الحسابات. تبقى قائمة حساباتك. متابعة؟|Esto quita el código y cierra la sesión de todas las cuentas. Tu lista de cuentas se conserva. ¿Continuar?|Cela supprime le code et vous déconnecte de tous les comptes. Votre liste de comptes reste. Continuer ?|Dadurch wird der Code entfernt und du wirst von allen Konten abgemeldet. Deine Kontoliste bleibt erhalten. Fortfahren?|Isso remove o código e desconecta você de todas as contas. Sua lista de contas permanece. Continuar?|Bu işlem parolayı kaldırır ve tüm hesaplardan çıkış yapar. Hesap listeniz kalır. Devam edilsin mi?|Код будет удалён, и вы выйдете из всех аккаунтов. Список аккаунтов сохранится. Продолжить?",
        "Passcode removed. Sign in again to each account.|تمت إزالة رمز المرور. سجّل الدخول من جديد في كل حساب.|Código eliminado. Vuelve a iniciar sesión en cada cuenta.|Code supprimé. Reconnectez-vous à chaque compte.|Code entfernt. Melde dich bei jedem Konto neu an.|Código removido. Entre novamente em cada conta.|Parola kaldırıldı. Her hesapta yeniden oturum açın.|Код удалён. Войдите заново в каждый аккаунт.",
        "Remove {0}?|هل تريد إزالة {0}؟|¿Quitar {0}?|Supprimer {0} ?|{0} entfernen?|Remover {0}?|{0} kaldırılsın mı?|Удалить {0}?",
        "Also delete its saved login and data from this PC?|هل تحذف أيضاً تسجيل دخوله وبياناته المحفوظة من هذا الجهاز؟|¿Borrar también su sesión y datos guardados en este PC?|Supprimer aussi sa connexion et ses données enregistrées sur ce PC ?|Auch die gespeicherte Anmeldung und Daten von diesem PC löschen?|Excluir também o login e os dados salvos deste PC?|Kayıtlı oturum ve verileri de bu bilgisayardan silinsin mi?|Удалить также сохранённый вход и данные с этого ПК?",
        "Optionally deletes its saved login and data too|يمكنك أيضاً حذف تسجيل دخوله وبياناته المحفوظة|Opcionalmente borra también su sesión y datos|Peut aussi supprimer sa connexion et ses données|Löscht optional auch Anmeldung und Daten|Opcionalmente exclui também o login e os dados|İsteğe bağlı olarak kayıtlı oturum ve verileri de siler|По желанию удаляет также сохранённый вход и данные",
        "Side bar with names|شريط جانبي بالأسماء|Barra lateral con nombres|Barre latérale avec noms|Seitenleiste mit Namen|Barra lateral com nomes|Adlı kenar çubuğu|Боковая панель с названиями",
        "Side bar, icons only|شريط جانبي بالأيقونات فقط|Barra lateral solo con iconos|Barre latérale, icônes seulement|Seitenleiste nur mit Symbolen|Barra lateral só com ícones|Kenar çubuğu, yalnızca simgeler|Боковая панель, только значки",
        "Expand or collapse the side bar (Ctrl+B)|وسّع الشريط الجانبي أو صغّره (Ctrl+B)|Expandir o contraer la barra lateral (Ctrl+B)|Agrandir ou réduire la barre latérale (Ctrl+B)|Seitenleiste erweitern oder einklappen (Strg+B)|Expandir ou recolher a barra lateral (Ctrl+B)|Kenar çubuğunu genişlet veya daralt (Ctrl+B)|Развернуть или свернуть боковую панель (Ctrl+B)",
        "Expand or collapse the side bar|وسّع الشريط الجانبي أو صغّره|Expandir o contraer la barra lateral|Agrandir ou réduire la barre latérale|Seitenleiste erweitern oder einklappen|Expandir ou recolher a barra lateral|Kenar çubuğunu genişlet veya daralt|Развернуть или свернуть боковую панель",
        "No downloads yet|لا توجد تنزيلات بعد|Aún no hay descargas|Aucun téléchargement pour l'instant|Noch keine Downloads|Nenhum download ainda|Henüz indirme yok|Загрузок пока нет",
        "New messages|رسائل جديدة|Mensajes nuevos|Nouveaux messages|Neue Nachrichten|Novas mensagens|Yeni mesajlar|Новые сообщения",
        "{0} new|{0} جديد|{0} nuevos|{0} nouveaux|{0} neu|{0} novas|{0} yeni|{0} нов.",
        "Settings|الإعدادات|Ajustes|Paramètres|Einstellungen|Configurações|Ayarlar|Настройки",
        "General|عام|General|Général|Allgemein|Geral|Genel|Общие",
        "Personalization|التخصيص|Personalización|Personnalisation|Anpassung|Personalização|Kişiselleştirme|Персонализация",
        "Notifications|الإشعارات|Notificaciones|Notifications|Benachrichtigungen|Notificações|Bildirimler|Уведомления",
        "Downloads|التنزيلات|Descargas|Téléchargements|Downloads|Downloads|İndirmeler|Загрузки",
        "Accounts|الحسابات|Cuentas|Comptes|Konten|Contas|Hesaplar|Аккаунты",
        "About|حول البرنامج|Acerca de|À propos|Info|Sobre|Hakkında|О программе",
        "Account settings|إعدادات الحساب|Ajustes de la cuenta|Paramètres du compte|Kontoeinstellungen|Configurações da conta|Hesap ayarları|Настройки аккаунта",
        "Language|اللغة|Idioma|Langue|Sprache|Idioma|Dil|Язык",
        "Follows Windows when available|يتبع لغة ويندوز عند توفرها|Sigue el idioma de Windows si existe|Suit la langue de Windows si possible|Folgt Windows, falls verfügbar|Segue o Windows quando disponível|Mümkünse Windows dilini kullanır|Как в Windows, если доступно",
        "Automatic|تلقائي|Automático|Automatique|Automatisch|Automático|Otomatik|Авто",
        "Restart LiteHub to apply the language everywhere.|أعد تشغيل LiteHub لتطبيق اللغة في كل مكان.|Reinicia LiteHub para aplicar el idioma en todas partes.|Redémarrez LiteHub pour appliquer la langue partout.|Starte LiteHub neu, um die Sprache überall anzuwenden.|Reinicie o LiteHub para aplicar o idioma em tudo.|Dilin her yerde uygulanması için LiteHub'ı yeniden başlatın.|Перезапустите LiteHub, чтобы язык применился везде.",
        "Startup|بدء التشغيل|Inicio|Démarrage|Autostart|Inicialização|Başlangıç|Автозапуск",
        "Launch LiteHub with Windows so notifications keep working|شغّل LiteHub مع ويندوز لتستمر الإشعارات|Inicia LiteHub con Windows para seguir recibiendo notificaciones|Lancer LiteHub avec Windows pour garder les notifications|LiteHub mit Windows starten, damit Benachrichtigungen funktionieren|Iniciar o LiteHub com o Windows para manter as notificações|Bildirimlerin çalışması için LiteHub'ı Windows ile başlat|Запускать LiteHub вместе с Windows, чтобы уведомления работали",
        "Disabled|معطّل|Desactivado|Désactivé|Aus|Desativado|Kapalı|Выкл.",
        "Enabled|مفعّل|Activado|Activé|An|Ativado|Açık|Вкл.",
        "Enabled, hidden in tray|مفعّل ومخفي في الشريط|Activado, oculto en la bandeja|Activé, masqué dans la zone de notification|An, im Infobereich versteckt|Ativado, oculto na bandeja|Açık, tepside gizli|Вкл., скрыт в трее",
        "Close button|زر الإغلاق|Botón de cierre|Bouton de fermeture|Schließen-Schaltfläche|Botão fechar|Kapat düğmesi|Кнопка закрытия",
        "Keep LiteHub running in the tray when you close the window|أبقِ LiteHub يعمل في الشريط عند إغلاق النافذة|Mantén LiteHub en la bandeja al cerrar la ventana|Garder LiteHub dans la zone de notification à la fermeture|LiteHub beim Schließen im Infobereich weiterlaufen lassen|Manter o LiteHub na bandeja ao fechar a janela|Pencere kapanınca LiteHub tepside çalışmaya devam etsin|Оставлять LiteHub в трее при закрытии окна",
        "Keyboard shortcut|اختصار لوحة المفاتيح|Atajo de teclado|Raccourci clavier|Tastenkürzel|Atalho de teclado|Klavye kısayolu|Горячая клавиша",
        "Quickly open or hide LiteHub from anywhere|افتح LiteHub أو أخفِه بسرعة من أي مكان|Abre u oculta LiteHub rápidamente desde cualquier lugar|Ouvrir ou masquer LiteHub depuis n'importe où|LiteHub von überall schnell öffnen oder ausblenden|Abra ou oculte o LiteHub rapidamente de qualquer lugar|LiteHub'ı her yerden hızlıca aç veya gizle|Быстро открывать или скрывать LiteHub откуда угодно",
        "Shortcut keys|مفاتيح الاختصار|Teclas del atajo|Touches du raccourci|Tastenkombination|Teclas do atalho|Kısayol tuşları|Клавиши",
        "Needs Alt, Ctrl, Shift or Win plus one key|يحتاج Alt أو Ctrl أو Shift أو Win مع مفتاح|Necesita Alt, Ctrl, Shift o Win más una tecla|Nécessite Alt, Ctrl, Maj ou Win plus une touche|Benötigt Alt, Strg, Umschalt oder Win plus eine Taste|Requer Alt, Ctrl, Shift ou Win mais uma tecla|Alt, Ctrl, Shift veya Win artı bir tuş gerekir|Нужен Alt, Ctrl, Shift или Win и ещё одна клавиша",
        "Record|تسجيل|Grabar|Enregistrer|Aufnehmen|Gravar|Kaydet|Записать",
        "Reset|إعادة ضبط|Restablecer|Réinitialiser|Zurücksetzen|Redefinir|Sıfırla|Сбросить",
        "Browse|استعراض|Examinar|Parcourir|Durchsuchen|Procurar|Gözat|Обзор",
        "Cancel|إلغاء|Cancelar|Annuler|Abbrechen|Cancelar|İptal|Отмена",
        "OK|موافق|Aceptar|OK|OK|OK|Tamam|OK",
        "Privacy|الخصوصية|Privacidad|Confidentialité|Datenschutz|Privacidade|Gizlilik|Конфиденциальность",
        "Passcode|رمز المرور|Código de acceso|Code d'accès|Zugangscode|Código de acesso|Parola|Код доступа",
        "Ask for a passcode each time LiteHub is launched|اطلب رمز المرور عند كل تشغيل لـ LiteHub|Pedir código cada vez que se inicia LiteHub|Demander un code à chaque lancement de LiteHub|Bei jedem Start von LiteHub einen Code abfragen|Pedir código a cada inicialização do LiteHub|LiteHub her açıldığında parola iste|Запрашивать код при каждом запуске LiteHub",
        "New passcode|رمز مرور جديد|Nuevo código|Nouveau code|Neuer Code|Novo código|Yeni parola|Новый код",
        "Type it again|اكتبه مرة أخرى|Escríbelo de nuevo|Saisissez-le à nouveau|Noch einmal eingeben|Digite novamente|Tekrar yazın|Введите ещё раз",
        "Current passcode|رمز المرور الحالي|Código actual|Code actuel|Aktueller Code|Código atual|Mevcut parola|Текущий код",
        "Enter your passcode|أدخل رمز المرور|Introduce tu código|Saisissez votre code|Code eingeben|Digite seu código|Parolanızı girin|Введите код доступа",
        "Theme|المظهر|Tema|Thème|Design|Tema|Tema|Тема",
        "System follows Windows|النظام يتبع ويندوز|Sistema sigue a Windows|Système suit Windows|System folgt Windows|Sistema segue o Windows|Sistem Windows'u izler|Система — как в Windows",
        "System|النظام|Sistema|Système|System|Sistema|Sistem|Система",
        "Light|فاتح|Claro|Clair|Hell|Claro|Açık|Светлая",
        "Dark|داكن|Oscuro|Sombre|Dunkel|Escuro|Koyu|Тёмная",
        "App layout|تخطيط البرنامج|Diseño|Disposition|Layout|Layout|Düzen|Расположение",
        "Account icons on the top bar or on the left|أيقونات الحسابات في الشريط العلوي أو على اليسار|Iconos de cuentas arriba o a la izquierda|Icônes des comptes en haut ou à gauche|Konto-Symbole oben oder links|Ícones das contas no topo ou à esquerda|Hesap simgeleri üstte veya solda|Значки аккаунтов сверху или слева",
        "Top navigation|تنقل علوي|Navegación superior|Navigation en haut|Navigation oben|Navegação superior|Üst gezinme|Сверху",
        "Left navigation|تنقل جانبي|Navegación izquierda|Navigation à gauche|Navigation links|Navegação esquerda|Sol gezinme|Слева",
        "Colorful menus|قوائم ملونة|Menús de colores|Menus colorés|Farbige Menüs|Menus coloridos|Renkli menüler|Цветные меню",
        "Show the selected account's color on the top bar|اعرض لون الحساب المحدد في الشريط العلوي|Muestra el color de la cuenta activa en la barra superior|Afficher la couleur du compte actif dans la barre du haut|Farbe des gewählten Kontos in der oberen Leiste zeigen|Mostrar a cor da conta selecionada na barra superior|Seçili hesabın rengini üst çubukta göster|Показывать цвет выбранного аккаунта на верхней панели",
        "Receive notifications|استقبال الإشعارات|Recibir notificaciones|Recevoir les notifications|Benachrichtigungen erhalten|Receber notificações|Bildirim al|Получать уведомления",
        "Allow accounts to show notifications for new messages|اسمح للحسابات بإظهار إشعارات الرسائل الجديدة|Permitir que las cuentas muestren notificaciones de mensajes nuevos|Autoriser les comptes à notifier les nouveaux messages|Konten dürfen bei neuen Nachrichten benachrichtigen|Permitir que as contas mostrem notificações de novas mensagens|Hesapların yeni mesaj bildirimi göstermesine izin ver|Разрешить аккаунтам показывать уведомления о новых сообщениях",
        "Badges|الشارات|Insignias|Pastilles|Zähler|Selos|Rozetler|Значки",
        "Account list|قائمة الحسابات|Lista de cuentas|Liste des comptes|Kontoliste|Lista de contas|Hesap listesi|Список аккаунтов",
        "Show a count next to each account icon|اعرض عدداً بجانب كل أيقونة حساب|Muestra un contador junto a cada icono|Afficher un compteur près de chaque icône|Zähler neben jedem Konto-Symbol anzeigen|Mostrar uma contagem ao lado de cada ícone|Her hesap simgesinin yanında sayı göster|Показывать счётчик у каждого значка",
        "Taskbar|شريط المهام|Barra de tareas|Barre des tâches|Taskleiste|Barra de tarefas|Görev çubuğu|Панель задач",
        "Show the total count on LiteHub's taskbar icon|اعرض العدد الإجمالي على أيقونة LiteHub في شريط المهام|Muestra el total en el icono de LiteHub de la barra de tareas|Afficher le total sur l'icône de LiteHub|Gesamtzahl am LiteHub-Symbol in der Taskleiste zeigen|Mostrar o total no ícone do LiteHub|Toplamı görev çubuğundaki LiteHub simgesinde göster|Показывать общее число на значке LiteHub",
        "Custom download location|مكان تنزيل مخصص|Ubicación de descarga personalizada|Dossier de téléchargement personnalisé|Eigener Download-Ordner|Local de download personalizado|Özel indirme konumu|Своя папка загрузок",
        "Choose where downloads are saved|اختر مكان حفظ التنزيلات|Elige dónde se guardan las descargas|Choisir où enregistrer les téléchargements|Speicherort für Downloads wählen|Escolha onde salvar os downloads|İndirmelerin nereye kaydedileceğini seçin|Выберите, куда сохранять загрузки",
        "Filter downloads list by account|صفِّ قائمة التنزيلات حسب الحساب|Filtrar descargas por cuenta|Filtrer les téléchargements par compte|Downloads nach Konto filtern|Filtrar downloads por conta|İndirme listesini hesaba göre süz|Фильтровать загрузки по аккаунту",
        "Only show downloads of the selected account when opening the list|اعرض تنزيلات الحساب المحدد فقط عند فتح القائمة|Solo muestra las descargas de la cuenta seleccionada|Afficher uniquement les téléchargements du compte sélectionné|Nur Downloads des gewählten Kontos zeigen|Mostrar só os downloads da conta selecionada|Listeyi açarken yalnızca seçili hesabın indirmelerini göster|Показывать загрузки только выбранного аккаунта",
        "Name|الاسم|Nombre|Nom|Name|Nome|Ad|Имя",
        "Account thumbnail|صورة الحساب|Miniatura de la cuenta|Vignette du compte|Konto-Bild|Miniatura da conta|Hesap simgesi|Значок аккаунта",
        "Service logo, or a colored letter|شعار الخدمة أو حرف ملون|Logo del servicio o una letra de color|Logo du service ou lettre colorée|Dienst-Logo oder farbiger Buchstabe|Logo do serviço ou letra colorida|Hizmet logosu veya renkli harf|Логотип сервиса или цветная буква",
        "Service logo|شعار الخدمة|Logo del servicio|Logo du service|Dienst-Logo|Logo do serviço|Hizmet logosu|Логотип сервиса",
        "Letter|حرف|Letra|Lettre|Buchstabe|Letra|Harf|Буква",
        "Platform icon|أيقونة المنصة|Icono de la plataforma|Icône de la plateforme|Plattform-Symbol|Ícone da plataforma|Platform simgesi|Значок платформы",
        "Download the icon again from the platform's website|نزّل الأيقونة مجدداً من موقع المنصة|Descarga de nuevo el icono desde el sitio de la plataforma|Retélécharger l'icône depuis le site de la plateforme|Symbol erneut von der Website der Plattform laden|Baixar o ícone novamente do site da plataforma|Simgeyi platformun sitesinden yeniden indir|Скачать значок заново с сайта платформы",
        "Refresh|تحديث|Actualizar|Actualiser|Aktualisieren|Atualizar|Yenile|Обновить",
        "Accent color|لون مميز|Color de acento|Couleur d'accent|Akzentfarbe|Cor de destaque|Vurgu rengi|Цвет акцента",
        "Show notifications for new messages|اعرض إشعارات الرسائل الجديدة|Mostrar notificaciones de mensajes nuevos|Afficher les notifications des nouveaux messages|Benachrichtigungen bei neuen Nachrichten zeigen|Mostrar notificações de novas mensagens|Yeni mesajlar için bildirim göster|Показывать уведомления о новых сообщениях",
        "Turned off for all accounts in Notifications|مغلقة لكل الحسابات من قسم الإشعارات|Desactivadas para todas las cuentas en Notificaciones|Désactivées pour tous les comptes dans Notifications|Für alle Konten unter Benachrichtigungen ausgeschaltet|Desativadas para todas as contas em Notificações|Bildirimler bölümünde tüm hesaplar için kapalı|Отключены для всех аккаунтов в разделе «Уведомления»",
        "Show this account's count in the account list and the taskbar badge|اعرض عدد هذا الحساب في القائمة وعلى شارة شريط المهام|Muestra el contador de esta cuenta en la lista y en la barra de tareas|Afficher le compteur de ce compte dans la liste et sur la barre des tâches|Zähler dieses Kontos in Liste und Taskleiste zeigen|Mostrar a contagem desta conta na lista e na barra de tarefas|Bu hesabın sayısını listede ve görev çubuğunda göster|Показывать счётчик этого аккаунта в списке и на панели задач",
        "Behavior|السلوك|Comportamiento|Comportement|Verhalten|Comportamento|Davranış|Поведение",
        "Keep running in background|أبقِه يعمل في الخلفية|Mantener en segundo plano|Garder en arrière-plan|Im Hintergrund weiterlaufen lassen|Manter em segundo plano|Arka planda çalışmaya devam et|Работать в фоне",
        "Needed to get notifications while you use another account|مطلوب لتصلك الإشعارات أثناء استخدام حساب آخر|Necesario para recibir notificaciones mientras usas otra cuenta|Nécessaire pour recevoir les notifications pendant que vous utilisez un autre compte|Nötig für Benachrichtigungen, während ein anderes Konto aktiv ist|Necessário para receber notificações enquanto usa outra conta|Başka bir hesap kullanırken bildirim almak için gerekli|Нужно, чтобы получать уведомления при работе с другим аккаунтом",
        "Open links in default browser|افتح الروابط في المتصفح الافتراضي|Abrir enlaces en el navegador predeterminado|Ouvrir les liens dans le navigateur par défaut|Links im Standardbrowser öffnen|Abrir links no navegador padrão|Bağlantıları varsayılan tarayıcıda aç|Открывать ссылки в браузере по умолчанию",
        "Links from messages open in your normal browser|الروابط من الرسائل تُفتح في متصفحك المعتاد|Los enlaces de los mensajes se abren en tu navegador habitual|Les liens des messages s'ouvrent dans votre navigateur habituel|Links aus Nachrichten öffnen sich im normalen Browser|Links das mensagens abrem no seu navegador normal|Mesajlardaki bağlantılar normal tarayıcınızda açılır|Ссылки из сообщений открываются в обычном браузере",
        "Choose where this account's downloads are saved|اختر مكان حفظ تنزيلات هذا الحساب|Elige dónde se guardan las descargas de esta cuenta|Choisir où enregistrer les téléchargements de ce compte|Speicherort für Downloads dieses Kontos wählen|Escolha onde salvar os downloads desta conta|Bu hesabın indirmelerinin kaydedileceği yeri seçin|Выберите, куда сохранять загрузки этого аккаунта",
        "Manage account|إدارة الحساب|Gestionar cuenta|Gérer le compte|Konto verwalten|Gerenciar conta|Hesabı yönet|Управление аккаунтом",
        "Remove|إزالة|Quitar|Supprimer|Entfernen|Remover|Kaldır|Удалить",
        "Remove account|إزالة الحساب|Quitar cuenta|Supprimer le compte|Konto entfernen|Remover conta|Hesabı kaldır|Удалить аккаунт",
        "Its saved login stays on disk until you delete the profile folder|يبقى تسجيل الدخول المحفوظ على القرص حتى تحذف مجلد الملف الشخصي|El inicio de sesión guardado permanece en disco hasta que borres la carpeta del perfil|La connexion enregistrée reste sur le disque jusqu'à la suppression du dossier du profil|Die gespeicherte Anmeldung bleibt auf der Platte, bis du den Profilordner löschst|O login salvo permanece no disco até você apagar a pasta do perfil|Kayıtlı oturum, profil klasörünü silene kadar diskte kalır|Сохранённый вход остаётся на диске, пока вы не удалите папку профиля",
        "Remove {0}? (its saved login stays on disk)|هل تريد إزالة {0}؟ (يبقى تسجيل دخوله محفوظاً على القرص)|¿Quitar {0}? (su inicio de sesión guardado queda en el disco)|Supprimer {0} ? (sa connexion enregistrée reste sur le disque)|{0} entfernen? (die gespeicherte Anmeldung bleibt auf der Platte)|Remover {0}? (o login salvo continua no disco)|{0} kaldırılsın mı? (kayıtlı oturum diskte kalır)|Удалить {0}? (сохранённый вход останется на диске)",
        "All your web accounts in one light window.\nEach account loads only when you open it and is freed when you stop using it.\n\nNo ads. No telemetry. Your logins stay on this PC.\nOpen source, MIT license.|كل حساباتك على الويب في نافذة واحدة خفيفة.\nيُحمَّل كل حساب عند فتحه فقط ويُحرَّر عند التوقف عن استخدامه.\n\nبلا إعلانات. بلا تتبّع. تسجيلات دخولك تبقى على جهازك.\nمفتوح المصدر، رخصة MIT.|Todas tus cuentas web en una ventana ligera.\nCada cuenta se carga solo al abrirla y se libera cuando dejas de usarla.\n\nSin anuncios. Sin telemetría. Tus sesiones se quedan en este PC.\nCódigo abierto, licencia MIT.|Tous vos comptes web dans une fenêtre légère.\nChaque compte ne se charge qu'à l'ouverture et se libère quand vous ne l'utilisez plus.\n\nSans pub. Sans télémétrie. Vos connexions restent sur ce PC.\nOpen source, licence MIT.|Alle deine Web-Konten in einem schlanken Fenster.\nJedes Konto lädt erst beim Öffnen und wird freigegeben, wenn du es nicht mehr nutzt.\n\nKeine Werbung. Keine Telemetrie. Deine Anmeldungen bleiben auf diesem PC.\nOpen Source, MIT-Lizenz.|Todas as suas contas web em uma janela leve.\nCada conta só carrega quando você a abre e é liberada quando deixa de usá-la.\n\nSem anúncios. Sem telemetria. Seus logins ficam neste PC.\nCódigo aberto, licença MIT.|Tüm web hesaplarınız tek bir hafif pencerede.\nHer hesap yalnızca açtığınızda yüklenir, kullanmayı bıraktığınızda serbest bırakılır.\n\nReklam yok. Telemetri yok. Oturumlarınız bu bilgisayarda kalır.\nAçık kaynak, MIT lisansı.|Все ваши веб-аккаунты в одном лёгком окне.\nКаждый аккаунт загружается только при открытии и освобождается, когда вы им не пользуетесь.\n\nБез рекламы. Без телеметрии. Ваши входы остаются на этом ПК.\nОткрытый код, лицензия MIT.",
        "Select a service|اختر خدمة|Elige un servicio|Choisir un service|Dienst auswählen|Escolha um serviço|Bir hizmet seçin|Выберите сервис",
        "You can add the same service many times\n(two WhatsApps, three Gmails...). Each one has\nits own login and can run at the same time.|يمكنك إضافة الخدمة نفسها عدة مرات\n(واتساب مرتين، جيميل ثلاث مرات...). لكل حساب\nتسجيل دخول خاص ويعمل في الوقت نفسه.|Puedes añadir el mismo servicio varias veces\n(dos WhatsApp, tres Gmail...). Cada uno tiene\nsu propia sesión y pueden funcionar a la vez.|Vous pouvez ajouter le même service plusieurs fois\n(deux WhatsApp, trois Gmail...). Chacun a\nsa propre connexion et ils fonctionnent en même temps.|Du kannst denselben Dienst mehrfach hinzufügen\n(zwei WhatsApp, drei Gmail...). Jedes Konto hat\neine eigene Anmeldung und läuft gleichzeitig.|Você pode adicionar o mesmo serviço várias vezes\n(dois WhatsApp, três Gmail...). Cada um tem\nseu próprio login e todos funcionam ao mesmo tempo.|Aynı hizmeti birçok kez ekleyebilirsiniz\n(iki WhatsApp, üç Gmail...). Her birinin\nkendi oturumu vardır ve aynı anda çalışır.|Один и тот же сервис можно добавить несколько раз\n(два WhatsApp, три Gmail...). У каждого свой\nвход, и все работают одновременно.",
        "Add account|إضافة حساب|Añadir cuenta|Ajouter un compte|Konto hinzufügen|Adicionar conta|Hesap ekle|Добавить аккаунт",
        "Add account  >|إضافة حساب  >|Añadir cuenta  >|Ajouter  >|Hinzufügen  >|Adicionar  >|Hesap ekle  >|Добавить  >",
        "Add another  >|إضافة آخر  >|Añadir otra  >|Ajouter un autre  >|Noch eins  >|Adicionar outra  >|Bir tane daha  >|Ещё один  >",
        "{0} added|{0} مضاف|{0} añadida(s)|{0} ajouté(s)|{0} hinzugefügt|{0} adicionada(s)|{0} eklendi|добавлено: {0}",
        "your own address|عنوانك الخاص|tu propia dirección|votre propre adresse|deine eigene Adresse|seu próprio endereço|kendi adresiniz|ваш собственный адрес",
        "Suggestions|اقتراحات|Sugerencias|Suggestions|Vorschläge|Sugestões|Öneriler|Рекомендуем",
        "Any other website|أي موقع آخر|Cualquier otro sitio web|Tout autre site|Jede andere Website|Qualquer outro site|Başka herhangi bir site|Любой другой сайт",
        "Custom website|موقع مخصص|Sitio web personalizado|Site personnalisé|Eigene Website|Site personalizado|Özel web sitesi|Свой сайт",
        "Messaging|مراسلة|Mensajería|Messagerie|Nachrichten|Mensagens|Mesajlaşma|Мессенджеры",
        "Email|بريد إلكتروني|Correo|E-mail|E-Mail|E-mail|E-posta|Почта",
        "Social|تواصل اجتماعي|Social|Réseaux sociaux|Soziales|Social|Sosyal|Соцсети",
        "Meetings|اجتماعات|Reuniones|Réunions|Meetings|Reuniões|Toplantılar|Встречи",
        "Office & files|مكتب وملفات|Oficina y archivos|Bureautique et fichiers|Büro & Dateien|Escritório e arquivos|Ofis ve dosyalar|Офис и файлы",
        "Productivity|الإنتاجية|Productividad|Productivité|Produktivität|Produtividade|Üretkenlik|Продуктивность",
        "Business & ads|أعمال وإعلانات|Negocios y publicidad|Business et publicité|Business & Werbung|Negócios e anúncios|İş ve reklam|Бизнес и реклама",
        "Developer|مطورون|Desarrollo|Développeur|Entwickler|Desenvolvedor|Geliştirici|Разработка",
        "AI|ذكاء اصطناعي|IA|IA|KI|IA|Yapay zeka|ИИ",
        "Music & video|موسيقى وفيديو|Música y vídeo|Musique et vidéo|Musik & Video|Música e vídeo|Müzik ve video|Музыка и видео",
        "Learning|تعلّم|Aprendizaje|Apprentissage|Lernen|Aprendizado|Öğrenme|Обучение",
        "Open|فتح|Abrir|Ouvrir|Öffnen|Abrir|Aç|Открыть",
        "Show in folder|إظهار في المجلد|Mostrar en la carpeta|Afficher dans le dossier|Im Ordner zeigen|Mostrar na pasta|Klasörde göster|Показать в папке",
        "Open downloads folder|فتح مجلد التنزيلات|Abrir carpeta de descargas|Ouvrir le dossier de téléchargements|Download-Ordner öffnen|Abrir pasta de downloads|İndirmeler klasörünü aç|Открыть папку загрузок",
        "Clear list|مسح القائمة|Vaciar lista|Vider la liste|Liste leeren|Limpar lista|Listeyi temizle|Очистить список",
        "File|الملف|Archivo|Fichier|Datei|Arquivo|Dosya|Файл",
        "Account|الحساب|Cuenta|Compte|Konto|Conta|Hesap|Аккаунт",
        "Status|الحالة|Estado|État|Status|Status|Durum|Статус",
        "Downloading|جارٍ التنزيل|Descargando|Téléchargement|Lädt|Baixando|İndiriliyor|Загрузка",
        "Done|تم|Listo|Terminé|Fertig|Concluído|Bitti|Готово",
        "Failed|فشل|Error|Échec|Fehlgeschlagen|Falhou|Başarısız|Ошибка",
        "Download finished|اكتمل التنزيل|Descarga completada|Téléchargement terminé|Download abgeschlossen|Download concluído|İndirme tamamlandı|Загрузка завершена",
        "Split view: show several accounts side by side|عرض مقسّم: أكثر من حساب جنباً إلى جنب|Vista dividida: varias cuentas lado a lado|Vue partagée : plusieurs comptes côte à côte|Geteilte Ansicht: mehrere Konten nebeneinander|Divisão: várias contas lado a lado|Bölünmüş görünüm: birkaç hesap yan yana|Разделённый вид: несколько аккаунтов рядом",
        "All accounts|كل الحسابات|Todas las cuentas|Tous les comptes|Alle Konten|Todas as contas|Tüm hesaplar|Все аккаунты",
        "Reload|إعادة تحميل|Recargar|Recharger|Neu laden|Recarregar|Yeniden yükle|Перезагрузить",
        "Open side by side|فتح جنباً إلى جنب|Abrir en paralelo|Ouvrir côte à côte|Nebeneinander öffnen|Abrir lado a lado|Yan yana aç|Открыть рядом",
        "Exit|خروج|Salir|Quitter|Beenden|Sair|Çıkış|Выход",
        "Single view|عرض واحد|Vista única|Vue simple|Einzelansicht|Visão única|Tek görünüm|Один экран",
        "2 side by side|اثنان جنباً إلى جنب|2 en paralelo|2 côte à côte|2 nebeneinander|2 lado a lado|2 yan yana|2 рядом",
        "3 columns|3 أعمدة|3 columnas|3 colonnes|3 Spalten|3 colunas|3 sütun|3 колонки",
        "2 x 2 (4 accounts)|2 × 2 (4 حسابات)|2 x 2 (4 cuentas)|2 x 2 (4 comptes)|2 x 2 (4 Konten)|2 x 2 (4 contas)|2 x 2 (4 hesap)|2 x 2 (4 аккаунта)",
        "3 x 2 (6 accounts)|3 × 2 (6 حسابات)|3 x 2 (6 cuentas)|3 x 2 (6 comptes)|3 x 2 (6 Konten)|3 x 2 (6 contas)|3 x 2 (6 hesap)|3 x 2 (6 аккаунтов)",
        "Six panes at most.|ستة أقسام كحد أقصى.|Máximo seis paneles.|Six volets au maximum.|Höchstens sechs Bereiche.|No máximo seis painéis.|En fazla altı bölme.|Не больше шести панелей.",
        "Use letters, numbers and dashes only.|استخدم أحرفاً وأرقاماً وشرطات فقط.|Usa solo letras, números y guiones.|Utilisez uniquement lettres, chiffres et tirets.|Nur Buchstaben, Zahlen und Bindestriche verwenden.|Use apenas letras, números e hifens.|Yalnızca harf, rakam ve tire kullanın.|Только буквы, цифры и дефисы.",
        "Your {0} name (the part of the address, e.g. mycompany)|اسم {0} الخاص بك (جزء العنوان، مثل mycompany)|Tu nombre de {0} (parte de la dirección, p. ej. miempresa)|Votre nom {0} (la partie de l'adresse, ex. monentreprise)|Dein {0}-Name (Teil der Adresse, z. B. meinefirma)|Seu nome {0} (parte do endereço, ex. minhaempresa)|{0} adınız (adresin bir parçası, örn. sirketim)|Ваше имя {0} (часть адреса, например mycompany)",
        "Account name (for example Work or Personal)|اسم الحساب (مثل العمل أو الشخصي)|Nombre de la cuenta (p. ej. Trabajo o Personal)|Nom du compte (ex. Travail ou Perso)|Kontoname (z. B. Arbeit oder Privat)|Nome da conta (ex. Trabalho ou Pessoal)|Hesap adı (örn. İş veya Kişisel)|Название аккаунта (например, Работа или Личный)",
        "Address (https://...)|العنوان (https://...)|Dirección (https://...)|Adresse (https://...)|Adresse (https://...)|Endereço (https://...)|Adres (https://...)|Адрес (https://...)",
        "Record shortcut|تسجيل اختصار|Grabar atajo|Enregistrer un raccourci|Tastenkürzel aufnehmen|Gravar atalho|Kısayol kaydet|Запись сочетания",
        "Press the new shortcut (needs Alt, Ctrl, Shift or Win + a key)|اضغط الاختصار الجديد (يحتاج Alt أو Ctrl أو Shift أو Win مع مفتاح)|Pulsa el nuevo atajo (necesita Alt, Ctrl, Shift o Win + una tecla)|Appuyez sur le nouveau raccourci (Alt, Ctrl, Maj ou Win + une touche)|Neues Kürzel drücken (Alt, Strg, Umschalt oder Win + Taste)|Pressione o novo atalho (Alt, Ctrl, Shift ou Win + uma tecla)|Yeni kısayola basın (Alt, Ctrl, Shift veya Win + bir tuş)|Нажмите новое сочетание (Alt, Ctrl, Shift или Win + клавиша)",
        "Another program already uses {0}. Pick a different shortcut.|برنامج آخر يستخدم {0} بالفعل. اختر اختصاراً مختلفاً.|Otro programa ya usa {0}. Elige otro atajo.|Un autre programme utilise déjà {0}. Choisissez un autre raccourci.|{0} wird bereits von einem anderen Programm verwendet. Wähle ein anderes Kürzel.|Outro programa já usa {0}. Escolha outro atalho.|{0} başka bir program tarafından kullanılıyor. Farklı bir kısayol seçin.|{0} уже используется другой программой. Выберите другое сочетание."
    };
}
