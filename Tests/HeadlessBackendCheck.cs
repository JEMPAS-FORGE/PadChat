using System;
class HeadlessBackendCheck {
 public static void Main(string[] args) {
  if(args.Length!=1)throw new ArgumentException("Supply the isolated shipping App directory.");
  // Use the actual production preflight: hidden child, all redirected handles,
  // immediately closed input and asynchronous output draining. No recording.
  for(int i=0;i<5;i++){Engine.VerifyBackend(args[0]);Console.WriteLine("PASS hidden shipping backend preflight "+(i+1));}
 }
}
