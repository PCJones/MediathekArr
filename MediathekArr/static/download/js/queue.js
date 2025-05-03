let currentInputId = null;
let initialIncompletePath = '';
let initialCompletePath = '';
let categories = [];
let initialCategories = [];
let categoriesOverridden = false;

async function fetchQueue() {
    try {
        const response = await fetch('/download/api?mode=queue');
        const data = await response.json();
        const slots = data.queue.slots || [];
        const tableBody = document.getElementById('queueBody');
        tableBody.innerHTML = ''; // Clear the table body

        slots.forEach(slot => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${slot.filename}</td>
                <td>${slot.status.replace('Extracting', 'Creating MKV')}</td>
                <td>
                    ${slot.mb === '0' || slot.mbleft === '0' ? '0' : Math.round(parseFloat(slot.mb.replace(',', '.')) - parseFloat(slot.mbleft.replace(',', '.')))} /
                    ${slot.mb === '0' ? 'Unknown' : Math.round(parseFloat(slot.mb.replace(',', '.')))} MB
                </td>
                <td>${slot.percentage}%</td>
            `;
            tableBody.appendChild(row);
        });
    } catch (error) {
        console.error('Error fetching queue:', error);
    }
}

function checkPathsBeforeWizard() {
    document.getElementById('pathCheckModal').style.display = 'block';
}

function proceedToWizard() {
    location.href = '/download/wizard.html';
}

async function openConfigModal() {
    const response = await fetch('/download/config');
    const data = await response.json();

    const { config, overrides } = data;

    // Update input values with the current config, replacing backslashes with forward slashes
    document.getElementById('incompletePath').value = config.incompletePath.replace(/\\/g, '/');
    document.getElementById('completePath').value = config.completePath.replace(/\\/g, '/');

    // Store initial values to detect changes later
    initialIncompletePath = config.incompletePath.replace(/\\/g, '/');
    initialCompletePath = config.completePath.replace(/\\/g, '/');

    // Handle categories
    categories = config.categories || [];
    initialCategories = [...categories]; // Create a copy
    categoriesOverridden = overrides.categories;
    renderCategories();

    const incompleteInput = document.getElementById('incompletePath');
    const completeInput = document.getElementById('completePath');
    const incompleteBrowseButton = document.getElementById('browseIncomplete');
    const completeBrowseButton = document.getElementById('browseComplete');
    const incompleteWarning = document.getElementById('incompletePathWarning');
    const completeWarning = document.getElementById('completePathWarning');
    const newCategoryInput = document.getElementById('newCategory');
    const categoriesAddButton = document.querySelector('button[onclick="addCategory()"]');
    const categoriesWarning = document.getElementById('categoriesWarning');

    // Handle Incomplete Path
    if (overrides.incompletePath) {
        incompleteInput.disabled = true;
        incompleteBrowseButton.disabled = true;
        incompleteWarning.style.display = 'block';
    } else {
        incompleteInput.disabled = false;
        incompleteBrowseButton.disabled = false;
        incompleteWarning.style.display = 'none';
    }

    // Handle Complete Path
    if (overrides.completePath) {
        completeInput.disabled = true;
        completeBrowseButton.disabled = true;
        completeWarning.style.display = 'block';
    } else {
        completeInput.disabled = false;
        completeBrowseButton.disabled = false;
        completeWarning.style.display = 'none';
    }

    // Handle Categories
    if (categoriesOverridden) {
        newCategoryInput.disabled = true;
        categoriesAddButton.disabled = true;
        categoriesWarning.style.display = 'block';
    } else {
        newCategoryInput.disabled = false;
        categoriesAddButton.disabled = false;
        categoriesWarning.style.display = 'none';
    }

    // Show the modal
    document.getElementById('configModal').style.display = 'block';
    document.getElementById('pathCheckModal').style.display = 'none';
}

function renderCategories() {
    const categoriesList = document.getElementById('categoriesList');
    categoriesList.innerHTML = '';

    if (categories.length === 0) {
        categoriesList.innerHTML = '<div style="padding: 10px; color: #666;">No categories defined</div>';
        return;
    }

    categories.forEach((category, index) => {
        const categoryDiv = document.createElement('div');
        categoryDiv.className = 'category-item';
        categoryDiv.style.display = 'flex';
        categoryDiv.style.justifyContent = 'space-between';
        categoryDiv.style.alignItems = 'center';
        categoryDiv.style.marginBottom = '5px';
        categoryDiv.style.padding = '5px';
        categoryDiv.style.backgroundColor = '#f4f4f4';
        categoryDiv.style.borderRadius = '3px';

        categoryDiv.innerHTML = `
            <span>${category}</span>
            <button type="button" ${categoriesOverridden ? 'disabled' : ''} 
                    onclick="removeCategory(${index})" 
                    style="padding: 2px 6px; background-color: #dc3545; color: white; min-width: 30px; margin: 0;">
                X
            </button>
        `;
        categoriesList.appendChild(categoryDiv);
    });
}
function addCategory() {
    const newCategoryInput = document.getElementById('newCategory');
    const newCategory = newCategoryInput.value.trim();

    if (newCategory && !categories.includes(newCategory)) {
        categories.push(newCategory);
        renderCategories();
        newCategoryInput.value = '';
    } else if (categories.includes(newCategory)) {
        alert('This category already exists!');
    }
}

function removeCategory(index) {
    categories.splice(index, 1);
    renderCategories();
}

async function browsePath(inputId) {
    currentInputId = inputId;

    // Attempt to load the directory from the current input field or default to "/"
    const currentPath = document.getElementById(inputId).value || '/';

    try {
        await loadDirectory(currentPath);
    } catch (error) {
        console.warn(`Failed to load directory "${currentPath}". Falling back to root.`);
        try {
            await loadDirectory('/'); // Fallback to root directory
        } catch (fallbackError) {
            console.error('Failed to load root directory:', fallbackError.message);
            alert('Failed to load directory. Please check your paths.');
        }
    }

    document.getElementById('fileBrowserModal').style.display = 'block';
}

async function loadDirectory(path) {
    try {
        const response = await fetch(`/download/browse?path=${encodeURIComponent(path.replace(/\\/g, '/'))}`);

        if (!response.ok) {
            throw new Error(`Directory "${path}" not found or cannot be accessed.`);
        }

        const data = await response.json();

        const container = document.getElementById('fileBrowserContents');
        const currentPath = data.currentPath.replace(/\\/g, '/');
        container.innerHTML = `<div><strong>Current Path:</strong> ${currentPath}</div>`;

        const table = document.createElement('table');
        table.style.width = '100%';
        table.style.borderCollapse = 'collapse';
        table.innerHTML = `
            <thead>
                <tr>
                    <th style="text-align: left; padding: 10px;">Icon</th>
                    <th style="text-align: left; padding: 10px;">Name</th>
                </tr>
            </thead>
            <tbody id="directoryTableBody"></tbody>
        `;
        container.appendChild(table);

        const tableBody = table.querySelector('#directoryTableBody');

        // Add "..." entry for navigating up, unless we are at "/"
        if (currentPath !== '/') {
            let parentPath;
            if (currentPath.endsWith('/')) {
                parentPath = currentPath.slice(0, -1);
            } else {
                parentPath = currentPath;
            }
            parentPath = parentPath.substring(0, parentPath.lastIndexOf('/')) || '/';

            // Ensure drive roots like "D:" properly resolve to "D:/"
            if (/^[A-Za-z]:$/.test(parentPath)) {
                parentPath += '/';
            }

            const parentRow = document.createElement('tr');
            parentRow.innerHTML = `
                <td style="padding: 10px;"><span style="font-size: 18px;">⬆</span></td>
                <td style="padding: 10px;"><a href="#" onclick="loadDirectory('${parentPath}')">...</a></td>
            `;
            tableBody.appendChild(parentRow);
        }

        // Add directories
        data.directories.forEach(dir => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td style="padding: 10px;"><span style="font-size: 18px;">📁</span></td>
                <td style="padding: 10px;"><a href="#" onclick="loadDirectory('${dir.path}')">${dir.name}</a></td>
            `;
            tableBody.appendChild(row);
        });

        // Add "Select This Directory" button
        const selectRow = document.createElement('tr');
        selectRow.innerHTML = `
            <td colspan="2" style="padding: 10px; text-align: center;">
                <button onclick="selectPath('${currentPath}')">Select This Directory</button>
            </td>
        `;
        tableBody.appendChild(selectRow);
    } catch (error) {
        console.error('Error loading directory:', error.message);
        alert('Error loading directory. Defaulting to /');
        throw error;
    }
}

function selectPath(path) {
    document.getElementById(currentInputId).value = path.replace(/\\/g, '/');
    closeFileBrowser();
}

function closeFileBrowser() {
    document.getElementById('fileBrowserModal').style.display = 'none';
}

function closeConfigModal(promptUser = true) {
    if (promptUser) {
        const incompletePath = document.getElementById('incompletePath').value;
        const completePath = document.getElementById('completePath').value;
        const categoriesChanged = JSON.stringify(categories) !== JSON.stringify(initialCategories);

        if (incompletePath !== initialIncompletePath || completePath !== initialCompletePath || categoriesChanged) {
            if (!confirm('You didn\'t save your changes - are you sure you want to close?')) {
                return;
            }
        }
    }
    document.getElementById('configModal').style.display = 'none';
}

async function saveConfig() {
    const incompletePath = document.getElementById('incompletePath').value;
    const completePath = document.getElementById('completePath').value;

    const response = await fetch('/download/config', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            incompletePath,
            completePath,
            categories
        }),
    });

    if (response.ok) {
        closeConfigModal(false);
    } else {
        alert('Failed to save configuration.');
    }
}

setInterval(fetchQueue, 5000);
window.onload = fetchQueue;
