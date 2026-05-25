import os
import re

pages_dir = "/home/nolax/Desktop/repos/PNET_Solokha_Danylo/PNET_Solokha_Danylo.Blazor/Components/Pages"
pages = ["Categories.razor", "Suppliers.razor", "SalesArchivePage.razor", "SystemAuditPage.razor"]

pagination_html_pattern = re.compile(r'<!-- Footer Pagination -->\s*<div class="d-flex flex-column flex-md-row align-items-center justify-content-between p-4 bg-light border-top gap-3">.*?</div>\s*</div>\s*</div>', re.DOTALL)
style_pattern = re.compile(r'<style>(.*?)</style>', re.DOTALL)

for p in pages:
    filepath = os.path.join(pages_dir, p)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # 1. Replace HTML block
    content = pagination_html_pattern.sub('<PaginationControl TotalCount="totalCount" Skip="skip" PageSize="take" OnPageChanged="HandlePageChanged" />\n                        </div>\n                    </div>\n                </div>', content)

    # 2. Extract styles
    styles = style_pattern.findall(content)
    if styles:
        for style_content in styles:
            if '.header-card' in style_content or '.header-info' in style_content:
                css_filepath = filepath + '.css'
                with open(css_filepath, 'w', encoding='utf-8') as cf:
                    cf.write(style_content.strip() + "\n")
        content = style_pattern.sub('', content)

    # 3. Add using statement
    if "@using PNET_Solokha_Danylo.Blazor.Components.Common" not in content:
        content = content.replace("@inject IMediator Mediator", "@using PNET_Solokha_Danylo.Blazor.Components.Common\n@inject IMediator Mediator")

    # 4. Replace pagination properties
    content = re.sub(r'\s*private int CurrentPage => \(skip \/ take\) \+ 1;\s*', '\n', content)
    content = re.sub(r'\s*private bool CanPrevPage => skip > 0;\s*', '\n', content)
    content = re.sub(r'\s*private bool CanNextPage => \(skip \+ take\) < totalCount;\s*', '\n', content)
    
    # 5. Replace methods
    # Delete PrevPage
    content = re.sub(r'\s*private async Task PrevPage\(\)\s*\{[^{}]*\{[^{}]*\}[^{}]*\}\s*', '\n', content)
    # Delete NextPage
    content = re.sub(r'\s*private async Task NextPage\(\)\s*\{[^{}]*\{[^{}]*\}[^{}]*\}\s*', '\n', content)
    # Delete OnPageSizeChanged
    content = re.sub(r'\s*private async Task OnPageSizeChanged\(ChangeEventArgs e\)\s*\{[^{}]*\{[^{}]*\}[^{}]*\}\s*', '\n', content)

    # Add HandlePageChanged before the last closing brace
    handle_page_changed = """
    private async Task HandlePageChanged((int Skip, int Take) pageInfo)
    {
        skip = pageInfo.Skip;
        take = pageInfo.Take;
        await FetchData(showSpinner: false);
    }
}
"""
    if "HandlePageChanged" not in content:
        # Check if FetchData has showSpinner argument
        if "FetchData(bool showSpinner" not in content:
             handle_page_changed = handle_page_changed.replace("await FetchData(showSpinner: false);", "await FetchData();")
        content = re.sub(r'}\s*$', handle_page_changed, content)

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

print("Safely refactored pages.")
