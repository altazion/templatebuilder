# templatebuilder
Outils de compilation pour réaliser des templates pour Altazion Commerce et Signage

Exemple d'utilisation : 

```yaml
steps:
  - name: Checkout
    uses: actions/checkout@v2   
  - name: Validate Template
    uses: altazion/templatebuilder@master
  - name: Create Release
    id: create_release        
    uses: actions/create-release@v1
    env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    with:
        tag_name: ${{ github.ref }}
        release_name: Release ${{ github.ref }}
        draft: false
        prerelease: false
  - name: Upload Template Zip
    id: upload-release-asset 
    uses: actions/upload-release-asset@v1
    env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    with:
        upload_url: ${{ steps.create_release.outputs.upload_url }}
        asset_path: final.zip
        asset_name: final.zip
        asset_content_type: application/zip
```




 




