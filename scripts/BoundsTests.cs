using System;
using System.IO;
using System.Linq;
using System.Text;
using SpineCadence;

class BoundsTests {
 static int checks;
 static void Assert(bool value,string name){if(!value)throw new Exception(name);checks++;}
 static byte[] Fixture(float x,float y,float width,float height,string version="4.1.24"){
  using(var stream=new MemoryStream()){
   stream.Write(new byte[8],0,8);byte[] text=Encoding.UTF8.GetBytes(version);stream.WriteByte((byte)(text.Length+1));stream.Write(text,0,text.Length);
   foreach(float value in new[]{x,y,width,height}){byte[] item=BitConverter.GetBytes(value);if(BitConverter.IsLittleEndian)Array.Reverse(item);stream.Write(item,0,4);}
   // Sentinel body makes accidental edits outside the bounds detectable.
   stream.Write(Enumerable.Range(0,128).Select(i=>(byte)i).ToArray(),0,128);return stream.ToArray();
  }
 }
 static void Reject(Action action,string name){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Assert(rejected,name);}
 static int Main(){string dir=Path.Combine(Path.GetTempPath(),"SpineBoundsTests_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try{
   string source=Path.Combine(dir,"source.skel"),target=Path.Combine(dir,"target.skel");
   File.WriteAllBytes(source,Fixture(-153.159f,-200.392f,287.299f,314.089f));var bounds=SkeletonBounds.ReadSource(source);
   byte[] before=Fixture(0,0,0,0);File.WriteAllBytes(target,before);string log=SkeletonBounds.Preserve(target,bounds);byte[] after=File.ReadAllBytes(target);
   var actual=SkeletonBounds.Read(after);Assert(actual.Bytes.SequenceEqual(bounds.Bytes),"non-square real bounds retained without rounding");
   Assert(after.Take(actual.Offset).SequenceEqual(before.Take(actual.Offset)),"hash and version unchanged");
   Assert(after.Skip(actual.Offset+16).SequenceEqual(before.Skip(actual.Offset+16)),"entire body unchanged");
   Assert(log.Contains("287.299")&&log.Contains("314.089"),"actual dimensions in record");
   SkeletonBounds.Preserve(target,bounds);Assert(after.SequenceEqual(File.ReadAllBytes(target)),"already correct binary unchanged");
   File.WriteAllBytes(target,Fixture(10,20,30,40));SkeletonBounds.Preserve(target,bounds);Assert(SkeletonBounds.Read(File.ReadAllBytes(target)).Bytes.SequenceEqual(bounds.Bytes),"nonzero wrong bounds corrected");
   File.WriteAllBytes(source,Fixture(0,0,0,0));Reject(()=>SkeletonBounds.ReadSource(source),"zero source rejected");
   File.WriteAllBytes(source,Fixture(0,0,10,0));Reject(()=>SkeletonBounds.ReadSource(source),"degenerate source rejected");
   Reject(()=>SkeletonBounds.Read(Fixture(0,0,-1,10)),"negative width rejected");
   Reject(()=>SkeletonBounds.Read(Fixture(float.NaN,0,10,10)),"NaN rejected");
   Reject(()=>SkeletonBounds.Read(Fixture(0,0,float.PositiveInfinity,10)),"infinity rejected");
   Reject(()=>SkeletonBounds.Read(Fixture(0,0,10,10,"4.2.00")),"wrong version rejected");
   Reject(()=>SkeletonBounds.Read(new byte[8]),"truncated hash rejected");
   Reject(()=>SkeletonBounds.Read(Fixture(0,0,10,10).Take(25).ToArray()),"truncated bounds rejected");
   var corrupt=new byte[20];for(int i=8;i<14;i++)corrupt[i]=255;Reject(()=>SkeletonBounds.Read(corrupt),"malformed varint rejected");
   File.WriteAllBytes(target,Fixture(0,0,10,10,"4.2.00"));byte[] unsupported=File.ReadAllBytes(target);Reject(()=>SkeletonBounds.Preserve(target,bounds),"wrong version never patched");Assert(unsupported.SequenceEqual(File.ReadAllBytes(target)),"rejected file unchanged");
   Console.WriteLine("PASS: "+checks+" bounds checks");return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  finally {Directory.Delete(dir,true);}
 }
}
