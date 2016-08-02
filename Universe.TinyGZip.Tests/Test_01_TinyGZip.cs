using System;
using System.Collections.Generic;
using System.Text;

namespace Universe.TineGZip.Tests
{
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.XPath;

    using NUnit.Framework;

    using TinyGZip;

    using CompressionMode = TinyGZip.CompressionMode;
    using TinyGZip = global::Universe.TinyGZip.GZipStream;

    [TestFixture]
    public class Test_01_TinyGZip : BaseTest
    {
        static Random rnd = new Random(1);

        [Test]
        public void Test_All()
        {
            var algRandom = new Func<int, byte[]>(GetRndom);
            var algWords = new Func<int, byte[]>(GetWords);
            
            foreach (var l in new[]{ 0,1,2,1111,111111,5000000 })
                for(int level = 0; level<=9; level++)
                    foreach (var alg in new[] {algRandom, algWords})
                    {
                        byte[] bytes = alg(l);
                        Try(bytes, (CompressionLevel) level, alg.Method.Name);
                    }

        }

        static void Try(byte[] arg, CompressionLevel level, string alg)
        {
            MemoryStream gzipped = new MemoryStream();
            using(TinyGZip gz = new TinyGZip(gzipped, CompressionMode.Compress, level, true))
                gz.Write(arg, 0, arg.Length);

            gzipped.Position = 0;
            MemoryStream copy = new MemoryStream();
            using (System.IO.Compression.GZipStream ungz = new System.IO.Compression.GZipStream(gzipped, System.IO.Compression.CompressionMode.Decompress, true))
                ungz.CopyTo(copy);

            var info = string.Format("Arg: {0,17} Level: {1}", arg.Length.ToString("n0") + " " + alg.Replace("Get", "") + " bytes", level);
            var expected = arg;
            var y = copy.ToArray();
            if (expected.Length != y.Length)
                Assert.Fail("Size distinguishes: {0}", info);

            for(int i=0; i<expected.Length; i++)
                if (expected[i] != y[i])
                    Assert.Fail("Byte[" + i + "] distinguishes");

            Trace.WriteLine(string.Format("Done: Compressed={0,10} | {1}", gzipped.Length.ToString("n0"), info));
        }

        static byte[] GetRndom(int length)
        {
            byte[] bytes = new byte[length];
            rnd.NextBytes(bytes);
            return bytes;
        }

        static byte[] GetWords(int length)
        {
            var words = LoremIpsum.Value;
            byte[] ret = new byte[length];
            int p = 0;
            while (p < length)
            {
                var word = words[rnd.Next(words.Length)];
                for (int i = 0; i < word.Length && p < length; i++)
                {
                    ret[p++] = (byte)(int)word[i];
                }
            }
            
            return ret;
        }

        static Lazy<string[]> LoremIpsum = new Lazy<string[]>(() =>
        {
            return RawLoremIpsum
                .Split(' ', '\r', '\n')
                .Where(x => x.Length > 0)
                .ToArray();
        });

        private static readonly string RawLoremIpsum = @"
Augue ponderum quo an, etiam primis consulatu an vis, te congue viderer has. Vel an tantas antiopam. Cum eu porro habemus! Recteque maluisset at sea, ne sea nostro verterem, usu iusto doctus vulputate an. At fabulas voluptua usu, ex vidit rebum appellantur nec.

 Vel ne errem vitae theophrastus. Eum no atqui vivendum disputando, nam ut prima putant. Indoctum moderatius ex vel. Mea id insolens moderatius, omittam prodesset delicatissimi ius te, agam pericula nam id. Quo labore eligendi noluisse at. Est quas vulputate te, eos eu prompta ancillae? Ut usu dictas propriae complectitur.

 At cum oporteat imperdiet. Vis labitur consequat ut, amet splendide an cum. In mea habemus vivendo eleifend, magna vivendum consequat his te, purto adhuc adipisci vim ei. Ea qui nostrum democritum. Ne accusam voluptua complectitur sea?

 Eum id natum choro quidam. Id hinc quot eam, scripta suscipit ad est? Ius cu eius debitis offendit, aperiri dolorum civibus ei pri? Ea esse viris usu! Eu pri sale nostro commune. Nec soleat virtute at.

 Liber movet minimum vim an, duo sint summo an. Errem feugait nam cu? Legere veritus an sea, nam justo maluisset et? Porro concludaturque cu vix, alii senserit no eam! Odio enim aperiri no sit. Ad vim erat justo eruditi, te modus tation nec. Nam errem volutpat id, sed ei adhuc recusabo, recteque erroribus quo ei.

 Ut minimum maiestatis reprehendunt has, case corpora qui ea. Ius assum decore minimum ad, quo lorem dicit ridens ex, ne alii prima mel. Phaedrum adipiscing his at. Eum rebum ubique constituam in, per minim feugiat ea. At ius nostro accusam persequeris, id cum novum putant maiestatis, pro modus labore deseruisse et. Sea ut liber inermis expetenda, oratio euismod mel te.

 Mel natum autem omittantur at, eos nostrud appareat ut? Mea inani dicit denique ea, ne consul docendi has. Mea justo luptatum delicatissimi eu, te labores expetendis interpretaris mea! Ei illud iisque oblique sea, vis noster laoreet voluptua no? Ut sit postea omittam, pri tritani fastidii placerat et. Eum albucius philosophia ne? Et mel nibh paulo omnium.

 Aeque viris accusam no vis, appetere theophrastus duo no, his at liber posidonium. Vix an etiam fierent, est ei veritus tractatos. Ad oblique nostrud eos, sea doctus periculis ut. Quo et illum munere everti! Mea brute dicam in, constituam neglegentur has cu, wisi accumsan id has! Suas everti vel cu?

 Ipsum etiam et eam. Qui at nibh placerat, ut zril tritani intellegebat nam? Vis ex sensibus accommodare, eu hinc nibh populo vix. Qui augue solet quidam cu. Ut error erroribus philosophia vis. Eos et ubique essent temporibus, ad natum solum gloriatur has! Sit enim scriptorem te, eu aeque necessitatibus eum, mei ex vitae sadipscing.

 Omnesque copiosae posidonium sed ne. An duo nostrum disputando. Sit et luptatum lobortis, sed inani consulatu scripserit in. An nam nisl oratio, vitae sensibus ocurreret ea vel? No erant invenire cum, et hinc efficiantur mea, sit an saperet ullamcorper.

 Sea laoreet vulputate complectitur ut, quod democritum an nec, cum an lobortis assueverit. Nominati vulputate constituto ius ea? Sit facete ocurreret et, cum tation sententiae reprehendunt no. Mea cu posse iudicabit, sit illud harum ei, ubique iuvaret interpretaris no sed.

 Ullum verear volumus ex per, an duo error vocent splendide. Sea quidam debitis deserunt in, vim similique eloquentiam suscipiantur ne. Cu ludus habemus mea, duo at putant appetere torquatos, eu mea graeci delenit oporteat? Probo blandit indoctum ne usu, at putent tractatos nec.

 Unum mutat eripuit te has. Duo epicurei qualisque et. Est assum inermis ne, ea pericula aliquando eum. Cum elitr eligendi neglegentur cu, epicuri detracto qui ei, te eos dicit voluptatum. Ex oratio veritus ius, ius cu natum atqui ignota? Has ea dicunt iracundia conceptam. Cu cetero laoreet pertinax sit, vel ne ancillae albucius sadipscing, eu offendit repudiare duo?

 Eos id solum dignissim, no posse intellegam suscipiantur duo! Pri luptatum signiferumque et, at nulla deterruisset eos? Quod tale ex pri, his ne hinc vocibus, ut pertinax expetendis eam! Mea at veritus nominati, ad solum pertinacia est. Timeam oblique legendos te pro, ad mel error forensibus. An liberavisse complectitur pro, sed nihil possim ceteros an.

 Mei in movet atomorum, vis eripuit dolorum fastidii in. Eripuit aliquam ne quo, mea et cetero commune! Facilis gubergren adipiscing vel ea. Solet nusquam fuisset has ut, ad mel probo fabellas! Cu prompta nominavi incorrupte sed. Ei purto vivendo vim?

 Ea debet epicurei eam, modus torquatos cu his, fugit facete persecuti vim id! In elaboraret delicatissimi vis. Tantas discere ne per, doming molestie an eum. Graeco voluptatibus mei ne, ea mei soluta ornatus assueverit. At eos tibique electram aliquando. Ius consul ponderum facilisis ut! Cu eum scaevola explicari, vis pertinax patrioque eu.

 At cum unum sadipscing. In virtute facilisi pri, tation bonorum ius no. An nam etiam possim explicari, laoreet splendide his at. Verterem oportere scribentur ei duo, ea veri vidit eum! Sea nisl impetus at. Quo ei hinc facete maiorum!

 Vocibus definiebas an est, no mazim voluptaria vim, wisi platonem ad has! Eum an facer evertitur, ius mazim partem ponderum cu, ei alii quas pri. Nisl verterem suavitate his no, has ut rebum ludus numquam, ad mea suavitate delicatissimi! Sed eu alii putant gloriatur, et illud alterum complectitur sed? Et putent expetenda est.

 Ne mei aperiam definiebas, integre temporibus mel ex, ea nec sint debet tollit. Alii delicatissimi eum ex, in vis ancillae consequat, sonet dictas meliore vis et. Ex pro homero latine aperiri! Scribentur reformidans in qui, nec elitr eleifend et. No paulo vivendum vix, pro id adipisci ullamcorper complectitur. Libris dictas id ius, tale molestie ut eam!

 Eum odio causae forensibus an, etiam minimum forensibus et pro? His no labores luptatum intellegat. Eam no amet enim persecuti. Eu his modus saepe efficiendi, cu mucius doming vim. Per ut quaeque scriptorem appellantur. Adhuc doming eam ne, pro ei veri ornatus inermis, id posse ponderum mea.

 Ius omnis dicta voluptatibus ad, nec id elitr noluisse. Movet detracto in mea, id pro tota intellegebat, an aeque oblique necessitatibus vel. Est movet laudem viderer cu, eam liber dolorum postulant et. Wisi omnes cu vis, brute alienum facilisis.";

    }
}
