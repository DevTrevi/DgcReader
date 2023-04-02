# DgcReader Test

#### Unit test class to validate the DgcReaderService

This unit test class uses the contents of the official [eu-digital-green-certificates/dgc-testdata](https://github.com/eu-digital-green-certificates/dgc-testdata) in order to verify the functionality of the `DgcReaderService`.  

#### How it works
The repository contains a number of test data supplied by each country, stored as json files in different folders.  
During the unit test initialization, every json file found in the subfolders of the repository is loaded, and its data used in the following ways:

- The `CERTIFICATE` field into the `TESTCTX` section is used to compose a TrustList used to validate the signatures of the test certificates. This allows to test the signature validation with the different key algoritms used by the different countries (ECC, RSA)
- The `PREFIX` field containing the RAW QrCode data is decoded, the signature verified with the test TrustList, and the DGC values verified with the assertions contained in the `EXPECTEDRESULTS` section.

#### Usage

1) Clone this repository on your machine.
2) Clone the [eu-digital-green-certificates/dgc-testdata](https://github.com/eu-digital-green-certificates/dgc-testdata) repostory too.
3) If you use the same base folder, you should be ready to execute the tests.  
Otherwise, if the root folder of the repositories are not in the same folder, you can edit the path to the ***dgc-testdata*** folder in the appsettings.json file
``` json
{
  "DgcTestDataRepositoryPath": "C:\\MyCustomPath\\dgc-testdata\\"
}
``` 
4) Run the unit tests. The project targets multiple frameworks, validating both the `System.Security.Cryptography` and the `Org.BouncyCastle` implementations.


------
Copyright &copy; 2021 Davide Trevisan  
Licensed under the Apache License, Version 2.0