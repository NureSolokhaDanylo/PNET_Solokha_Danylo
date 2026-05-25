import os
import re

pages = ["Categories.razor", "Suppliers.razor", "SalesArchivePage.razor", "SystemAuditPage.razor"]
pages_dir = "/home/nolax/Desktop/repos/PNET_Solokha_Danylo/PNET_Solokha_Danylo.Blazor/Components/Pages"

methods_to_remove = r"private int CurrentPage.*?(?=^\}|$)"

replacement = """
    private async Task HandlePageChanged((int Skip, int Take) pageInfo)
    {
        skip = pageInfo.Skip;
        take = pageInfo.Take;
        await FetchData(showSpinner: false);
    }
}"""

replacement_fallback = """
    private async Task HandlePageChanged((int Skip, int Take) pageInfo)
    {
        skip = pageInfo.Skip;
        take = pageInfo.Take;
        await FetchData();
    }
}"""

for p in pages:
    filepath = os.path.join(pages_dir, p)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check if we have FetchData(bool showSpinner)
    has_spinner = "FetchData(bool showSpinner" in content
    
    # We remove from "private int CurrentPage" down to the last closing brace
    # Actually, simpler: replace the known block.
    # The block is usually:
    # private int CurrentPage => (skip / take) + 1;
    # private bool CanPrevPage => skip > 0;
    # ...
    # private async Task OnPageSizeChanged(ChangeEventArgs e) { ... }
    
    pattern = re.compile(r'private int CurrentPage\s*=>\s*\(skip / take\) \+ 1;.*?(?=^\}$)', re.DOTALL | re.MULTILINE)
    
    rep = replacement if has_spinner else replacement_fallback
    
    new_content = pattern.sub(rep, content)
    
    # Add @using PNET_Solokha_Danylo.Blazor.Components.Common if it's missing
    if "@using PNET_Solokha_Danylo.Blazor.Components.Common" not in new_content:
        new_content = new_content.replace("@inject IMediator Mediator", "@using PNET_Solokha_Danylo.Blazor.Components.Common\n@inject IMediator Mediator")

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)

print("Done fixing code blocks")
