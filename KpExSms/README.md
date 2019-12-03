Попытка доработать KpSms для получения данных из SMS

Сигнал 3 - данные первых 8 символов SMS в качестве данных, отображение в Scada в формате ASCII текст
Сигнал 4 - телефонный номер без "+" формат числовой D

Используя номер тел. отправителя и данные из SMS можно использовать Модуль автоматического управления для дальнейших действий.
Либо логику Scada системы

Для получения числового значения из SMS необходимо число использовать первым, потом пробел и далее текст
Пример:

-15,4 Температура за бортом.

Для удаления всего кроме числа в Справочник - Формулы добавить

// Формула -------------------------------------
public static double ParseToDouble(double value)
{
  double result = Double.NaN;

  string s = ScadaUtils.DecodeAscii(value);
  int idx = s.IndexOf(" ");
  if (idx != -1)
  {
  s = s.Substring(0, idx);
  }

  if (!double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("ru-RU"), out result))
  {
     if (!double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("en-US"), out result))
     {
        return Double.NaN;
     }
  }
  return result;
}
// Конец формулы -----------------------------

Можно в качестве разделителя использовать как точку так и запятую
