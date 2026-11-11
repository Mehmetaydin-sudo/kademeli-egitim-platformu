using System.Windows.Forms;

namespace KademeliEgitimPlatformu
{
    /// <summary>
    /// Yeniden çizim sırasında titremeyi azaltan çift tamponlu panel.
    /// Düşük donanımlı bilgisayarlarda özel çizimli kartların akıcı kalmasını sağlar.
    /// </summary>
    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
