// Cytoscape.js interop for Blazor
let cy = null;
let isInitializing = false;

window.cytoscapeInterop = {
    initialize: function (containerId, nodes, edges) {
        // Prevent concurrent initialization
        if (isInitializing) {
            console.log('Cytoscape initialization already in progress');
            return false;
        }

        isInitializing = true;

        // Safely destroy existing instance first
        this.destroy();

        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            isInitializing = false;
            return false;
        }

        // Ensure container has dimensions
        if (container.offsetWidth === 0 || container.offsetHeight === 0) {
            console.warn('Container has no dimensions');
            isInitializing = false;
            return false;
        }

        // Check if we have nodes to display
        if (!nodes || nodes.length === 0) {
            console.log('No nodes to display');
            isInitializing = false;
            return false;
        }

        // Check if cytoscape library is loaded
        if (typeof cytoscape === 'undefined') {
            console.error('Cytoscape library not loaded');
            isInitializing = false;
            return false;
        }

        // Define color palette for entity types
        const typeColors = {
            'person': '#1890ff',
            'organization': '#52c41a',
            'geo': '#fa8c16',
            'location': '#fa8c16',
            'event': '#722ed1',
            'technology': '#13c2c2',
            'product': '#eb2f96',
            'concept': '#faad14',
            'date': '#2f54eb',
            'time': '#2f54eb',
            'default': '#8c8c8c'
        };

        const getColor = (type) => typeColors[(type || '').toLowerCase()] || typeColors['default'];

        // Prepare elements
        const elements = [];
        const nodeIds = new Set();

        nodes.forEach(node => {
            if (node && node.id) {
                nodeIds.add(node.id);
                elements.push({
                    data: {
                        id: node.id,
                        label: node.label || node.id,
                        type: node.type || 'default',
                        description: node.description || '',
                        color: getColor(node.type)
                    }
                });
            }
        });

        // Only add edges where both source and target exist
        if (edges && edges.length > 0) {
            edges.forEach(edge => {
                if (edge && edge.source && edge.target && nodeIds.has(edge.source) && nodeIds.has(edge.target)) {
                    elements.push({
                        data: {
                            id: `e-${edge.source}-${edge.target}-${Math.random().toString(36).substring(2, 11)}`,
                            source: edge.source,
                            target: edge.target,
                            label: edge.label || '',
                            weight: edge.weight || 1
                        }
                    });
                }
            });
        }

        try {
            // Initialize Cytoscape with preset layout (no animation during init)
            cy = cytoscape({
                container: container,
                elements: elements,
                style: [
                    {
                        selector: 'node',
                        style: {
                            'background-color': 'data(color)',
                            'label': 'data(label)',
                            'color': '#262626',
                            'font-size': '12px',
                            'text-valign': 'bottom',
                            'text-margin-y': 8,
                            'width': 40,
                            'height': 40,
                            'border-width': 2,
                            'border-color': '#fff',
                            'text-wrap': 'ellipsis',
                            'text-max-width': '80px'
                        }
                    },
                    {
                        selector: 'node:selected',
                        style: {
                            'border-width': 4,
                            'border-color': '#1890ff',
                            'width': 50,
                            'height': 50
                        }
                    },
                    {
                        selector: 'edge',
                        style: {
                            'width': 2,
                            'line-color': '#bfbfbf',
                            'target-arrow-color': '#bfbfbf',
                            'target-arrow-shape': 'triangle',
                            'curve-style': 'bezier',
                            'label': 'data(label)',
                            'font-size': '10px',
                            'color': '#8c8c8c',
                            'text-rotation': 'autorotate',
                            'text-margin-y': -10
                        }
                    },
                    {
                        selector: 'edge:selected',
                        style: {
                            'width': 3,
                            'line-color': '#1890ff',
                            'target-arrow-color': '#1890ff'
                        }
                    }
                ],
                layout: { name: 'preset' }, // Use preset to avoid double layout
                minZoom: 0.2,
                maxZoom: 3,
                wheelSensitivity: 0.3
            });

            // Run cose layout after initialization
            if (cy && !cy.destroyed()) {
                cy.layout({
                    name: 'cose',
                    animate: true,
                    animationDuration: 500,
                    nodeRepulsion: 8000,
                    idealEdgeLength: 100,
                    gravity: 0.3,
                    numIter: 1000,
                    stop: function() {
                        isInitializing = false;
                    }
                }).run();
            } else {
                isInitializing = false;
            }

            console.log('Cytoscape initialized with', nodes.length, 'nodes and', (edges ? edges.length : 0), 'edges');
            return true;
        } catch (error) {
            console.error('Error initializing cytoscape:', error);
            cy = null;
            isInitializing = false;
            return false;
        }
    },

    getSelectedNode: function () {
        if (!cy || cy.destroyed()) return null;
        try {
            const selected = cy.nodes(':selected');
            if (selected.length === 0) return null;
            const node = selected[0];
            return {
                id: node.id(),
                label: node.data('label'),
                type: node.data('type'),
                description: node.data('description')
            };
        } catch (e) {
            console.warn('Error getting selected node:', e);
            return null;
        }
    },

    fitGraph: function () {
        if (cy && !cy.destroyed()) {
            try { cy.fit(50); } catch (e) { console.warn('Error fitting graph:', e); }
        }
    },

    resetZoom: function () {
        if (cy && !cy.destroyed()) {
            try { cy.zoom(1); cy.center(); } catch (e) { console.warn('Error resetting zoom:', e); }
        }
    },

    runLayout: function (layoutName) {
        if (!cy || cy.destroyed()) return;
        try {
            cy.layout({ name: layoutName || 'cose', animate: true }).run();
        } catch (e) {
            console.warn('Error running layout:', e);
        }
    },

    destroy: function () {
        if (cy) {
            try {
                if (!cy.destroyed()) {
                    cy.destroy();
                }
            } catch (e) {
                console.warn('Error destroying cytoscape:', e);
            }
            cy = null;
        }
    }
};

